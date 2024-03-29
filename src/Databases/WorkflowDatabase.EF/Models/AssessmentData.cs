﻿using System;

namespace WorkflowDatabase.EF.Models
{
    public class AssessmentData
    {
        public int AssessmentDataId { get; set; }
        public int PrimarySdocId { get; set; }
        public string RsdraNumber { get; set; }
        public string SourceDocumentName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public DateTime? ToSdoDate { get; set; }
        public DateTime? EffectiveStartDate { get; set; }
        public string TeamDistributedTo { get; set; }
        public string SourceDocumentType { get; set; }
        public string SourceNature { get; set; }
        public string Datum { get; set; }
        public int ProcessId { get; set; }

        public string ParsedRsdraNumber => this.RsdraNumber.Replace("RSDRA", "");
    }
}
