﻿CREATE TABLE [dbo].[Users]
(
	[Id] UNIQUEIDENTIFIER CONSTRAINT PK_Users_Id PRIMARY KEY,
	[Name] NVARCHAR(100) NOT NULL,
	[Email] NVARCHAR(100) NOT NULL,
	[LastLoggedOn] DATETIME2 NOT NULL,
	[CreatedBy] UNIQUEIDENTIFIER NOT NULL,
	[CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedOn DEFAULT GETUTCDATE(),
    [UpdatedBy] UNIQUEIDENTIFIER NULL, 
    [UpdatedOn] DATETIME2 NULL
)
