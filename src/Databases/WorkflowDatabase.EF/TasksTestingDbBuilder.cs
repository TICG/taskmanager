﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.EF
{
    public class TasksDbBuilder : ICanCreateTables, ICanPopulateTables, ICanSaveChanges
    {
        private readonly WorkflowDbContext _context;

        private TasksDbBuilder(WorkflowDbContext context)
        {
            _context = context;

            if (_context.Database.GetDbConnection().State == ConnectionState.Closed)
            {
                _context.Database.OpenConnection();
            }

            // Schema hack to use generated SQL Server SQL with SQL Lite
            RunSql(new RawSqlString("ATTACH DATABASE ':memory:' AS dbo"));
        }

        public static ICanCreateTables UsingDbContext(WorkflowDbContext context)
        {
            return new TasksDbBuilder(context);
        }

        public ICanPopulateTables CreateTables()
        {
            var sqlTablesRootPath = AppDomain.CurrentDomain.BaseDirectory;
            var tables = Directory.GetFiles(Path.Combine(sqlTablesRootPath, @"SqlTables"));

            foreach (var table in tables)
            {
                RunSql(new RawSqlString(File.ReadAllText(table)));
            }
            
            return this;
        }

        private void RunSql(RawSqlString sqlString)
        {
            // Not ideal mixing SQL with EF
            _context.Database.ExecuteSqlCommand(sqlString);
            _context.SaveChanges();
        }

        public ICanSaveChanges PopulateTables()
        {
            if (!File.Exists(@"Data\ProcessesSeedData.json")) return this;
            if (!File.Exists(@"Data\TasksSeedData.json")) return this;

            var jsonString = File.ReadAllText(@"Data\ProcessesSeedData.json");
            var processes = JsonConvert.DeserializeObject<IEnumerable<Process>>(jsonString);
            _context.Processes.AddRange(processes);
            
            jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
            var tasks = JsonConvert.DeserializeObject<IEnumerable<Task>>(jsonString);

            _context.Tasks.AddRange(tasks);

            return this;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }

    public interface ICanSaveChanges
    {
        void SaveChanges();
    }

    public interface ICanPopulateTables
    {
        ICanSaveChanges PopulateTables();
    }

    public interface ICanCreateTables
    {
        ICanPopulateTables CreateTables();
    }
}
