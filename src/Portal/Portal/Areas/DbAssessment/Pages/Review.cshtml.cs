﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Areas.DbAssessment.Pages
{
    public class ReviewModel : PageModel
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public int ProcessId { get; set; }
        public _TaskInformationModel TaskInformationModel { get; set; }
        public _AssignTaskModel AssignTaskModel { get; set; }
        public _CommentsModel CommentsModel { get; set; }
        public WorkflowDbContext DbContext { get; set; }

        public ReviewModel(WorkflowDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            DbContext = dbContext;
        }

        public void OnGet(int processId)
        {
            ProcessId = processId;

            TaskInformationModel = SetTaskInformationData(processId);
            AssignTaskModel = SetAssignTaskData();
        }

        public IActionResult OnGetRetrieveComments(int processId)
        {
            var model = new _CommentsModel()
            {
                Comments = DbContext.Comment.Where(c => c.ProcessId == processId).ToList(),
                ProcessId = processId
            };

            // Repopulate models...
            OnGet(processId);

            return new PartialViewResult
            {
                ViewName = "_Comments",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                }
            };
        }

        public IActionResult OnGetCommentsPartial(string comment, int processId)
        {
            // TODO: Test with Azure
            // TODO: This will not work in Azure; need alternative; but will work in local dev
            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;

            DbContext.Comment.Add(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = DbContext.WorkflowInstance.First(c => c.ProcessId == processId).WorkflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userId) ? "Unknown" : userId,
                Text = comment
            });

            DbContext.SaveChanges();

            return OnGetRetrieveComments(processId);
        }

        private _TaskInformationModel SetTaskInformationData(int processId)
        {
            if (!System.IO.File.Exists(@"Data\SourceCategories.json")) throw new FileNotFoundException(@"Data\SourceCategories.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\SourceCategories.json");
            var sourceCategories = JsonConvert.DeserializeObject<IEnumerable<SourceCategory>>(jsonString);

            return new _TaskInformationModel
            {
                ProcessId = processId,
                DmEndDate = DateTime.Now,
                DmReceiptDate = DateTime.Now,
                EffectiveReceiptDate = DateTime.Now,
                ExternalEndDate = DateTime.Now,
                OnHold = 4,
                Ion = "2929",
                ActivityCode = "1272",
                SourceCategory = new SourceCategory { SourceCategoryId = 1, Name = "zzzzz" },
                SourceCategories = new SelectList(
                        sourceCategories, "SourceCategoryId", "Name")
            };
        }

        private _AssignTaskModel SetAssignTaskData()
        {
            return new _AssignTaskModel
            {
                Assessor = new Assessor { AssessorId = 1, Name = "Peter Bates" },
                Assessors = new SelectList(
                    new List<Assessor>
                    {
                        new Assessor {AssessorId = 0, Name = "Brian Stenson"},
                        new Assessor {AssessorId = 1, Name = "Peter Bates"}
                    }, "AssessorId", "Name"),
                SourceType = new SourceType { SourceTypeId = 0, Name = "Simple" },
                SourceTypes = new SelectList(
                    new List<SourceType>
                    {
                        new SourceType{SourceTypeId = 0, Name = "Simple"},
                        new SourceType{SourceTypeId = 1, Name = "LTA (Product only)"},
                        new SourceType{SourceTypeId = 2, Name = "LTA"}
                    }, "SourceTypeId", "Name"),
                Verifier = new Verifier { VerifierId = 1, Name = "Matt Stoodley" },
                Verifiers = new SelectList(
                    new List<Verifier>
                    {
                        new Verifier{VerifierId = 0, Name = "Brian Stenson"},
                        new Verifier{VerifierId = 1, Name = "Matt Stoodley"},
                        new Verifier{VerifierId = 2, Name = "Peter Bates"}
                    }, "VerifierId", "Name")
            };
        }
    }
}