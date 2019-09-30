﻿using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Areas.DbAssessment.Pages
{
    public class _AssignTaskModel : PageModel
    {
        [DisplayName("Assessor:")]
        public Assessor Assessor { get; set; }
        public SelectList Assessors { get; set; }

        [DisplayName("Verifier:")]
        public Verifier Verifier { get; set; }
        public SelectList Verifiers { get; set; }

        [DisplayName("Source Type:")]
        public SourceType SourceType { get; set; }
        public SelectList SourceTypes { get; set; }

        [DisplayName("Notes:")]
        public string Notes { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }

        public void OnGet()
        {
        }
    }
}