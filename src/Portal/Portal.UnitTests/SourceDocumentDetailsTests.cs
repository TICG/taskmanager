﻿using System;
using System.Threading.Tasks;
using Common.Factories;
using Common.Helpers;
using Common.Messages.Enums;
using Common.Messages.Events;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Portal.Configuration;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class SourceDocumentDetailsTests
    {
        private WorkflowDbContext _dbContext;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private DocumentStatusFactory _documentStatusFactory;
        private _SourceDocumentDetailsModel _sourceDocumentDetailsModel;
        private IDataServiceApiClient _fakeDataServiceApiClient;

        private int ProcessId { get; set; }
        private int SdocId { get; set; }
        private Guid CorrelationId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _documentStatusFactory = new DocumentStatusFactory(_dbContext);

            ProcessId = 123;
            SdocId = 123456;
            CorrelationId = Guid.NewGuid();

            _sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, new OptionsSnapshotWrapper<UriConfig>(new UriConfig()), _fakeEventServiceApiClient, _fakeDataServiceApiClient, _documentStatusFactory);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public void Test_InvalidOperationException_thrown_when_no_assessmentdata_exists()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "ActName",
                AssessmentData = null,
                SerialNumber = "123_sn",
                Status = "Started",
                WorkflowType = "DbAssessment"
            });

            _dbContext.SaveChanges();

            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, null, null, null, null);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                sourceDocumentDetailsModel.OnGet());
            Assert.AreEqual("Unable to retrieve AssessmentData", ex.Data["OurMessage"]);
        }

        [Test]
        public void Test_no_exception_thrown_when_no_primarydocumentstatus_row_exists()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "AnActName",
                AssessmentData = null,
                SerialNumber = "123_sn",
                Status = "Started",
                WorkflowType = "DbAssessment"
            });
            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = ProcessId,
                PrimarySdocId = SdocId,
                SourceDocumentName = "MyName",
                RsdraNumber = "12345",
                ReceiptDate = DateTime.Now,
                EffectiveStartDate = DateTime.Now,
                SourceNature = "Au naturale",
                Datum = "What",
                SourceDocumentType = "This",
                TeamDistributedTo = "HW"
            });
            _dbContext.SaveChanges();

            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, null, null, null, null) { ProcessId = ProcessId };
            Assert.DoesNotThrow(() => sourceDocumentDetailsModel.OnGet());
        }

        [Test]
        public async Task Test_DatabaseDocumentStatusProcessor_Is_Used_To_Create_A_New_DatabaseDocumentStatus_Row()
        {
            await _sourceDocumentDetailsModel.OnPostAddSourceFromSdraAsync(SdocId, "", "", ProcessId, Guid.NewGuid());

            Assert.IsNotNull(_dbContext.DatabaseDocumentStatus.FirstAsync(dds => dds.ProcessId == ProcessId && dds.SdocId == SdocId));
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_Is_Fired_When_Adding_Source_From_Database()
        {
            await _sourceDocumentDetailsModel.OnPostAddSourceFromSdraAsync(SdocId, "", "", ProcessId, CorrelationId);

            A.CallTo(() => _fakeEventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent), A<InitiateSourceDocumentRetrievalEvent>.Ignored))
                .MustHaveHappened();
        }
    }
}
