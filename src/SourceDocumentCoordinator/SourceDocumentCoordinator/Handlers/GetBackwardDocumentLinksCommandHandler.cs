﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetBackwardDocumentLinksCommandHandler : IHandleMessages<GetBackwardDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        public GetBackwardDocumentLinksCommandHandler(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }

        public async Task Handle(GetBackwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docLinks = await _dataServiceApiClient.GetBackwardDocumentLinks(message.SourceDocumentId);
            var linkedDocIds = docLinks.Select(s => s.DocId1).ToList(); //Backward Linked will have DocId2 as sdocId, while DocId1 as LinkedSdocId

            if (linkedDocIds?.Count == 0) return;

            foreach (var linkedDocId in linkedDocIds)
            {
                var documentAssessmentData = await _dataServiceApiClient.GetAssessmentData(linkedDocId);

                var linkedDocument = new LinkedDocuments
                {
                    ProcessId = message.ProcessId,
                    PrimarySdocId = message.SourceDocumentId,
                    LinkedSdocId = documentAssessmentData.SdocId,
                    RsdraNumber = documentAssessmentData.SourceName,
                    SourceDocumentName = documentAssessmentData.Name,
                    ReceiptDate = documentAssessmentData.ReceiptDate,
                    SourceDocumentType = documentAssessmentData.DocumentType,
                    SourceNature = documentAssessmentData.SourceName,
                    Datum = documentAssessmentData.Datum,
                    LinkType = "Backward",
                    Status = LinkedDocumentRetrievalStatus.NotAttached.ToString(),
                    Created = DateTime.Now
                };

                _dbContext.Add(linkedDocument);
                _dbContext.SaveChanges();
            }
        }
    }
}
