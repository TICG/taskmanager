﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{

    [Table("LinkedDocument")]
    public class LinkedDocument
    {
        public int LinkedDocumentId { get; set; }
        public int SdocId { get; set; }
        public string RsdraNumber { get; set; }
        public string SourceDocumentName { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string SourceDocumentType { get; set; }
        public string SourceNature { get; set; }
        public string Datum { get; set; }
        public string LinkType { get; set; }
        public int LinkedSdocId { get; set; }
        public DateTime Created { get; set; }

        public string ParsedRsdraNumber => this.RsdraNumber.Replace("RSDRA", "");
    }
}