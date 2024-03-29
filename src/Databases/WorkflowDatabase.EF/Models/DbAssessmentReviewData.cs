﻿namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentReviewData
    {
        public int DbAssessmentReviewDataId { get; set; }
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        public string TaskComplexity { get; set; }
        public int WorkflowInstanceId { get; set; }
    }
}
