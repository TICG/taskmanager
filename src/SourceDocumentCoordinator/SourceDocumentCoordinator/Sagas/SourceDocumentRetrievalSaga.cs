﻿using Common.Helpers;
using Common.Messages.Commands;

using Microsoft.Extensions.Options;

using NServiceBus;
using NServiceBus.Logging;

using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;

using System;
using System.Linq;
using System.Threading.Tasks;
using DataServices.Models;
using SourceDocumentCoordinator.Enums;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSaga : Saga<SourceDocumentRetrievalSagaData>,
        IAmStartedByMessages<InitiateSourceDocumentRetrievalCommand>,
        IHandleTimeouts<GetDocumentRequestQueueStatusCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        ILog log = LogManager.GetLogger<SourceDocumentRetrievalSaga>();

        public SourceDocumentRetrievalSaga(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient,
            IOptionsSnapshot<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _generalConfig = generalConfig;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SourceDocumentRetrievalSagaData> mapper)
        {
            mapper.ConfigureMapping<InitiateSourceDocumentRetrievalCommand>(message => message.SourceDocumentId)
                .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public async Task Handle(InitiateSourceDocumentRetrievalCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(InitiateSourceDocumentRetrievalCommand)}: {message.ToJSONSerializedString()}");

            if (!Data.IsStarted)
            {
                Data.IsStarted = true;
                Data.CorrelationId = message.CorrelationId;
                Data.ProcessId = message.ProcessId;
                Data.SourceDocumentId = message.SourceDocumentId;
                Data.SourceDocumentStatusId = 0;
            }

            // Call GetDocumentForViewing method on DataServices API
            var returnCode = await _dataServiceApiClient.GetDocumentForViewing(_generalConfig.Value.CallerCode,
                message.SourceDocumentId,
                _generalConfig.Value.SourceDocumentWriteableFolderName,
                message.GeoReferenced);

            ProcessSourceDocumentErrorCodes(returnCode, message);

            if (Data.SourceDocumentStatusId > 0)
            {
                var requestStatus = new GetDocumentRequestQueueStatusCommand
                {
                    SourceDocumentId = message.SourceDocumentId,
                    CorrelationId = message.CorrelationId
                };

                await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context,
                    TimeSpan.FromSeconds(_generalConfig.Value.SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                    requestStatus);
            }
        }

        public async Task Timeout(GetDocumentRequestQueueStatusCommand message, IMessageHandlerContext context)
        {
            var queuedDocs = _dataServiceApiClient.GetDocumentRequestQueueStatus(_generalConfig.Value.CallerCode);

            // TODO: Potentially deal with a list of queued requests...
            var sourceDocument = queuedDocs.Result.First(x => x.SodcId == message.SourceDocumentId);

            if (sourceDocument.Code == null)
                throw new ApplicationException(
                    $"Source Document Retrieval Status Code is null {Environment.NewLine}{sourceDocument.ToJSONSerializedString()}");

            await ProcessSourceDocumentQueueStatuses(message, context, sourceDocument);
        }

        private async Task ProcessSourceDocumentQueueStatuses(GetDocumentRequestQueueStatusCommand message,
            IMessageHandlerContext context, QueuedDocumentObject sourceDocument)
        {
            switch ((RequestQueueStatusReturnCodeEnum)sourceDocument.Code.Value)
            {
                case RequestQueueStatusReturnCodeEnum.Success:
                    // Doc Ready; update DB;
                    UpdateSourceDocumentStatus(message);

                    var removeFromQueue = new ClearDocumentRequestFromQueueCommand
                    {
                        CorrelationId = message.CorrelationId,
                        SourceDocumentId = message.SourceDocumentId
                    };
                    await context.SendLocal(removeFromQueue).ConfigureAwait(false);

                    // Fire command to store source doc in Content Service
                    var persistCommand = new PersistDocumentInStoreCommand
                    {
                        CorrelationId = message.CorrelationId,
                        SourceDocumentId = message.SourceDocumentId,
                        Filepath = sourceDocument.Message
                    };
                    await context.SendLocal(persistCommand).ConfigureAwait(false);

                    MarkAsComplete();

                    break;
                case RequestQueueStatusReturnCodeEnum.Queued:
                    // Still queued; fire another timer
                    await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context,
                        TimeSpan.FromSeconds(_generalConfig.Value
                            .SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                        message);
                    break;
                case RequestQueueStatusReturnCodeEnum.ConversionFailed:
                case RequestQueueStatusReturnCodeEnum.ConversionTimeOut:
                case RequestQueueStatusReturnCodeEnum.NotSuitableForConversion:
                case RequestQueueStatusReturnCodeEnum.NotGeoreferenced:
                
                    MarkAsComplete();

                    var msg = new InitiateSourceDocumentRetrievalCommand
                    {
                        CorrelationId = message.CorrelationId,
                        ProcessId = Data.ProcessId,
                        SourceDocumentId = message.SourceDocumentId,
                        GeoReferenced = false
                    };
                    await context.SendLocal(msg);
                    break;
                case RequestQueueStatusReturnCodeEnum.FolderNotWritable:
                    MarkAsComplete();
                    throw new ApplicationException($"Source document folder not writeable: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                case RequestQueueStatusReturnCodeEnum.QueueInsertionFailed:
                    MarkAsComplete();
                    throw new ApplicationException($"Unable to queue source document for retrieval: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                default:
                    MarkAsComplete();
                    throw new NotImplementedException($"sourceDocument.Code: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                      $"{sourceDocument.Code}");
            }
        }

        private void ProcessSourceDocumentErrorCodes(ReturnCode returnCode, InitiateSourceDocumentRetrievalCommand message)
        {
            switch ((QueueForRetrievalReturnCodeEnum)returnCode.Code.Value)
            {
                case QueueForRetrievalReturnCodeEnum.Success:
                    Data.SourceDocumentStatusId = AddSourceDocumentStatus(message);
                    break;
                case QueueForRetrievalReturnCodeEnum.AlreadyQueued:
                    if (Data.SourceDocumentStatusId < 1)
                    {
                        Data.SourceDocumentStatusId = AddSourceDocumentStatus(message);
                    }
                    break;
                case QueueForRetrievalReturnCodeEnum.QueueInsertionFailed:
                    MarkAsComplete();
                    throw new ApplicationException($"Unable to queue source document for retrieval: {Environment.NewLine}{returnCode.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                case QueueForRetrievalReturnCodeEnum.SdocIdNotRecognised:
                    MarkAsComplete();
                    throw new ApplicationException($"Source document Id not recognised when queuing document for retrieval: {Environment.NewLine}{returnCode.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                default:
                    MarkAsComplete();
                    throw new NotImplementedException($"Return code from GetDocumentForViewing not implemented: {Environment.NewLine}{returnCode.Message}{Environment.NewLine}" +
                                                      $"{returnCode}");
            }
        }

        private int AddSourceDocumentStatus(InitiateSourceDocumentRetrievalCommand message)
        {
            var sourceDocumentStatus = new SourceDocumentStatus
            {
                ProcessId = message.ProcessId,
                SdocId = message.SourceDocumentId,
                Status = SourceDocumentRetrievalStatus.Started.ToString(),
                StartedAt = DateTime.Now
            };

            _dbContext.SourceDocumentStatus.Add(sourceDocumentStatus);

            _dbContext.SaveChanges();

            return sourceDocumentStatus.SourceDocumentStatusId;
        }

        private void UpdateSourceDocumentStatus(GetDocumentRequestQueueStatusCommand message)
        {
            var sourceDocumentStatus =
                _dbContext.SourceDocumentStatus.FirstOrDefault(s => s.SdocId == message.SourceDocumentId);
            if (sourceDocumentStatus != null)
            {
                sourceDocumentStatus.Status = SourceDocumentRetrievalStatus.Ready.ToString();
                _dbContext.SaveChanges();
            }
        }
    }
}
