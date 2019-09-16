﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
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
                Data.SourceDocumentId = message.SourceDocumentId;
                Data.SourceDocumentStatusId = 0;
            }

            // Call GetDocumentForViewing method on DataServices API
            var returnCode = await _dataServiceApiClient.GetDocumentForViewing(_generalConfig.Value.CallerCode, message.SourceDocumentId, "tbc",
                true);

            // TODO: Think about different return code scenarios
            if (returnCode.Code.HasValue 
                && (returnCode.Code.Value == 0)
                || (returnCode.Code.Value == 1 && Data.SourceDocumentStatusId == 0))
            {
                var sourceDocumentStatus = new SourceDocumentStatus
                {
                    ProcessId = message.ProcessId,
                    SdocId = message.SourceDocumentId,
                    Status = "Started",
                    StartedAt = DateTime.Now
                };

                _dbContext.SourceDocumentStatus.Add(sourceDocumentStatus);

                _dbContext.SaveChanges();

                Data.SourceDocumentStatusId = sourceDocumentStatus.SourceDocumentStatusId;
            }

            var requestStatus = new GetDocumentRequestQueueStatusCommand
            {
                SdocId = message.SourceDocumentId,
                CorrelationId = message.CorrelationId
            };

            await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context, 
                TimeSpan.FromSeconds(_generalConfig.Value.SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                requestStatus);

            // TODO: Subsequent stories:
            // Once document has been fetched, call ClearDocumentRequestJobFromQueue on DataServices API and close saga...

            //MarkAsComplete();
        }

        public async Task Timeout(GetDocumentRequestQueueStatusCommand message, IMessageHandlerContext context)
        {
            var queuedDocs = _dataServiceApiClient.GetDocumentRequestQueueStatus(_generalConfig.Value.CallerCode);

            // TODO: Potentially deal with a list of queued requests...
            var ours = queuedDocs.Result.First(x => x.SodcId == message.SdocId);

            // TODO: if code != 1 etc...
            if (ours.Code == 1)
            {
                await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context, 
                    TimeSpan.FromSeconds(_generalConfig.Value.SourceDocumentCoordinatorQueueStatusIntervalSeconds), 
                    message);
            }
        }
    }
}