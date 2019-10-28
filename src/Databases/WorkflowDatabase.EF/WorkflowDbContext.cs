﻿using System;
using System.Data.SqlClient;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.EF
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
            : base(options)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "";
            var isLocalDevelopment = environmentName.Equals("LocalDevelopment", StringComparison.OrdinalIgnoreCase);

            if (!isLocalDevelopment)
            {
                (this.Database.GetDbConnection() as SqlConnection).AccessToken = new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;
            }
        }

        public DbSet<AssessmentData> AssessmentData { get; set; }
        public DbSet<Comments> Comment { get; set; }
        public DbSet<DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstance { get; set; }
        public DbSet<SourceDocumentStatus> SourceDocumentStatus { get; set; }
        public DbSet<LinkedDocument> LinkedDocument { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowInstance>().HasKey(x => x.WorkflowInstanceId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.Comment);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.SourceDocumentStatus)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<SourceDocumentStatus>(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.DbAssessmentReviewData);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(p => p.AssessmentData)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<AssessmentData>(p => p.ProcessId);

            modelBuilder.Entity<Comments>().HasKey(x => x.CommentId);

            modelBuilder.Entity<SourceDocumentStatus>().HasKey(x => x.SourceDocumentStatusId);

            modelBuilder.Entity<AssessmentData>().HasKey(x => x.AssessmentDataId);
            modelBuilder.Entity<AssessmentData>()
                .HasMany(x => x.LinkedDocuments)
                .WithOne()
                .HasPrincipalKey(p => p.SdocId)
                .HasForeignKey(p => p.SdocId);

            modelBuilder.Entity<LinkedDocument>().HasKey(x => x.LinkedDocumentId);

            base.OnModelCreating(modelBuilder);
        }
    }
}