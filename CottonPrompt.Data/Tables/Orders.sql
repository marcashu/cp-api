﻿CREATE TABLE [dbo].[Orders]
(
	[Id] INT CONSTRAINT PK_OrderId PRIMARY KEY IDENTITY(1,1),
	[OrderNumber] NVARCHAR(50) NOT NULL,
	[Priority] BIT NOT NULL,
	[Concept] NVARCHAR(MAX) NOT NULL,
	[PrintColorId] INT NOT NULL CONSTRAINT FK_Orders_OrderPrintColors REFERENCES [dbo].[OrderPrintColors]([Id]),
	[DesignBracketId] INT NOT NULL CONSTRAINT FK_Orders_OrderDesignBrackets REFERENCES [dbo].[OrderDesignBrackets]([Id]),
	[OutputSizeId] INT NOT NULL CONSTRAINT FK_Orders_OrderOutputSizes REFERENCES [dbo].[OrderOutputSizes]([Id]),	
	[UserGroupId] INT NOT NULL CONSTRAINT FK_Orders_UserGroups REFERENCES [dbo].[UserGroups]([Id]),
	[CustomerEmail] NVARCHAR(100) NOT NULL, 
	[CustomerStatus] NVARCHAR(50) NULL,
	[ArtistId] UNIQUEIDENTIFIER NULL CONSTRAINT FK_Orders_Artists REFERENCES [dbo].[Users]([Id]),
	[ArtistStatus] NVARCHAR(50) NULL,
	[CheckerId] UNIQUEIDENTIFIER NULL CONSTRAINT FK_Orders_Checkers REFERENCES [dbo].[Users]([Id]),
	[CheckerStatus] NVARCHAR(50) NULL,
	[CompletedOn] DATETIME2 NULL,
	[OriginalOrderId] INT NULL CONSTRAINT FK_Orders_OriginalOrder REFERENCES [dbo].[Orders]([Id]),
	[ChangeRequestOrderId] INT NULL CONSTRAINT FK_Orders_ChangeRequestOrder REFERENCES [dbo].[Orders]([Id]),
    [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
	[CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedOn DEFAULT GETUTCDATE(),
    [UpdatedBy] UNIQUEIDENTIFIER NULL, 
    [UpdatedOn] DATETIME2 NULL
)
