﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using DataServices.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Handlers;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.UnitTests
{
    public class GetForwardDocumentLinksCommandHandlerTests
    {
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private TestableMessageHandlerContext _handlerContext;
        private GetForwardDocumentLinksCommandHandler _handler;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();

            _handlerContext = new TestableMessageHandlerContext();
            _handler = new GetForwardDocumentLinksCommandHandler(_dbContext, _fakeDataServiceApiClient);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_expected_linkdocument_saved_to_dbcontext()
        {
            //Given
            var message = new GetForwardDocumentLinksCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999
            };

            var assessmentData = new WorkflowDatabase.EF.Models.AssessmentData()
            {
                PrimarySdocId = message.SourceDocumentId,
                RsdraNumber = "RSDRA2019000130865"
            };
            await _dbContext.AssessmentData.AddAsync(assessmentData);
            await _dbContext.SaveChangesAsync();

            var docLinks = new LinkedDocuments()
            {
                new LinkedDocument()
                {
                    DocId1 = message.SourceDocumentId,
                    DocId2 = 9888888,
                    LinkType = "PARENTCHILD"
                }
            };
            A.CallTo(() => _fakeDataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId)).Returns(docLinks);

            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(9888888)).Returns(new DocumentAssessmentData()
            {
                SourceName = "RSDRA2019000130872"
            });

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(1, _dbContext.LinkedDocument.Count());
            Assert.AreEqual("RSDRA2019000130872", _dbContext.LinkedDocument.First().RsdraNumber);
            Assert.AreEqual("Forward", _dbContext.LinkedDocument.First().LinkType);
        }

        [Test]
        public async Task Test_when_no_linkeddocument_nothing_saved_to_db()
        {
            //Given
            var message = new GetForwardDocumentLinksCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 1234,
                SourceDocumentId = 1888403
            };

            var assessmentData = new WorkflowDatabase.EF.Models.AssessmentData()
            {
                PrimarySdocId = message.SourceDocumentId,
                RsdraNumber = "RSDRA2017000130865"
            };
            await _dbContext.AssessmentData.AddAsync(assessmentData);
            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeDataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId)).Returns(new LinkedDocuments());

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            CollectionAssert.IsEmpty(_dbContext.LinkedDocument);
        }

    }
}
