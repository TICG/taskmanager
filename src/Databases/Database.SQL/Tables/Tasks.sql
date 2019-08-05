﻿CREATE TABLE [dbo].[Tasks]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [ProcessId] VARCHAR(50) NULL, 
    [DaysToDmEndDate] SMALLINT NULL, 
    [DmEndDate] DATETIME2 NULL, 
    [DaysOnHold] SMALLINT NULL, 
    [RsdraNo] NCHAR(10) NULL, 
    [SourceName] NCHAR(10) NULL, 
    [Workspace] NCHAR(10) NULL, 
    [TaskType] NCHAR(10) NULL, 
    [TaskStage] NCHAR(10) NULL, 
    [Assessor] NCHAR(10) NULL, 
    [Verifier] NCHAR(10) NULL, 
    [Team] NCHAR(10) NULL, 
    [TaskNote] NCHAR(100) NULL
)