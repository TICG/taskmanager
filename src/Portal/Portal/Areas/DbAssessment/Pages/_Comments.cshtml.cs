using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDatabase.EF.Models;

namespace Portal.Areas.DbAssessment.Pages
{
    public class _CommentsModel : PageModel
    {
        public int ProcessId { get; set; }
        public List<Comments> Comments { get; set; }

        public void OnGet()
        {

        }

    }
}