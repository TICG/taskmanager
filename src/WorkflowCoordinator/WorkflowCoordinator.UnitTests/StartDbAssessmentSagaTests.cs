using AutoMapper;

using FakeItEasy;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NServiceBus.Testing;

using NUnit.Framework;

using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using Common.Messages.Events;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Sagas;

using WorkflowDatabase.EF;

namespace WorkflowCoordinator.UnitTests
{
    public class StartDbAssessmentSagaTests
    {
        private StartDbAssessmentSaga _saga;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private TestableMessageHandlerContext _handlerContext;
        private WorkflowDbContext _dbContext;
        private IMapper _fakeMapper;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            var generalConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            var generalConfig = new GeneralConfig { WorkflowCoordinatorAssessmentPollingIntervalSeconds = 5, CallerCode = "HDB" };
            A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeMapper = A.Fake<IMapper>();

            _saga = new StartDbAssessmentSaga(generalConfigOptionsSnapshot,
                _fakeDataServiceApiClient,
                _fakeWorkflowServiceApiClient,
                _dbContext,
                _fakeMapper)
            { Data = new StartDbAssessmentSagaData() };
            _handlerContext = new TestableMessageHandlerContext();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Saves_Saga_Data()
        {
            //Given
            var correlationId = Guid.NewGuid();
            var sourceDocumentId = 99;

            //When
            await _saga.Handle(new StartDbAssessmentCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sourceDocumentId
            }, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _saga.Data.CorrelationId);
            Assert.AreEqual(sourceDocumentId, _saga.Data.SourceDocumentId);
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_RetrieveAssessmentDataCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var retrieveAssessmentDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is RetrieveAssessmentDataCommand);
            Assert.IsNotNull(retrieveAssessmentDataCommand, $"No message of type {nameof(RetrieveAssessmentDataCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_GetBackwardDocumentLinksCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var getBackwardDocumentLinksCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is GetBackwardDocumentLinksCommand);
            Assert.IsNotNull(getBackwardDocumentLinksCommand, $"No message of type {nameof(GetBackwardDocumentLinksCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_GetForwardDocumentLinksCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var getForwardDocumentLinksCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is GetForwardDocumentLinksCommand);
            Assert.IsNotNull(getForwardDocumentLinksCommand, $"No message of type {nameof(GetForwardDocumentLinksCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_GetSepDocumentLinksCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var getSepDocumentLinksCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is GetSepDocumentLinksCommand);
            Assert.IsNotNull(getSepDocumentLinksCommand, $"No message of type {nameof(GetSepDocumentLinksCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Publishes_InitiateSourceDocumentRetrievalCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var initiateSourceDocumentRetrievalEvent = _handlerContext.PublishedMessages.SingleOrDefault(t =>
                t.Message is InitiateSourceDocumentRetrievalEvent);
            Assert.IsNotNull(initiateSourceDocumentRetrievalEvent, $"No message of type {nameof(InitiateSourceDocumentRetrievalEvent)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_1_Messages()
        {
            //Given
            
            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            Assert.AreEqual(4, _handlerContext.SentMessages.Length);
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Publishes_1_Messages()
        {
            //Given
            
            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            Assert.AreEqual(1, _handlerContext.PublishedMessages.Length);
        }
    }
}