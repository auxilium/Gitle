/*
   donderdag 26 mei 201614:54:23
   User: 
   Server: STATLER
   Database: Gitle
   Application: 
*/

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.Correction
	DROP CONSTRAINT FK3232CB7E49E95258
GO
ALTER TABLE dbo.Invoice SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
BEGIN TRANSACTION
GO
CREATE TABLE dbo.Tmp_Correction
	(
	Id bigint NOT NULL IDENTITY (1, 1),
	Guid uniqueidentifier NULL,
	IsActive bit NULL,
	Description nvarchar(255) NULL,
	Price decimal(18, 2) NULL,
	Invoice_id bigint NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_Correction SET (LOCK_ESCALATION = TABLE)
GO
SET IDENTITY_INSERT dbo.Tmp_Correction ON
GO
IF EXISTS(SELECT * FROM dbo.Correction)
	 EXEC('INSERT INTO dbo.Tmp_Correction (Id, Guid, IsActive, Description, Price, Invoice_id)
		SELECT Id, Guid, IsActive, Description, CONVERT(decimal(18, 2), Price), Invoice_id FROM dbo.Correction WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_Correction OFF
GO
DROP TABLE dbo.Correction
GO
EXECUTE sp_rename N'dbo.Tmp_Correction', N'Correction', 'OBJECT' 
GO
ALTER TABLE dbo.Correction ADD CONSTRAINT
	PK__Correcti__3214EC076477ECF3 PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.Correction ADD CONSTRAINT
	FK3232CB7E49E95258 FOREIGN KEY
	(
	Invoice_id
	) REFERENCES dbo.Invoice
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
COMMIT
