/*
   donderdag 26 mei 201614:51:50
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
ALTER TABLE dbo.Invoice
	DROP CONSTRAINT FKDC60817FEFE956F
GO
ALTER TABLE dbo.[User] SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.Invoice
	DROP CONSTRAINT FKDC60817C4985D11
GO
ALTER TABLE dbo.Project SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
BEGIN TRANSACTION
GO
CREATE TABLE dbo.Tmp_Invoice
	(
	Id bigint NOT NULL IDENTITY (1, 1),
	Guid uniqueidentifier NULL,
	IsActive bit NULL,
	CreatedAt datetime NULL,
	HourPrice decimal(18, 2) NULL,
	Title nvarchar(255) NULL,
	Number nvarchar(255) NULL,
	VAT bit NULL,
	Remarks nvarchar(255) NULL,
	State nvarchar(255) NULL,
	StartDate datetime NULL,
	EndDate datetime NULL,
	Project_id bigint NULL,
	CreatedBy_id bigint NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_Invoice SET (LOCK_ESCALATION = TABLE)
GO
SET IDENTITY_INSERT dbo.Tmp_Invoice ON
GO
IF EXISTS(SELECT * FROM dbo.Invoice)
	 EXEC('INSERT INTO dbo.Tmp_Invoice (Id, Guid, IsActive, CreatedAt, HourPrice, Title, Number, VAT, Remarks, State, StartDate, EndDate, Project_id, CreatedBy_id)
		SELECT Id, Guid, IsActive, CreatedAt, CONVERT(decimal(18, 2), HourPrice), Title, Number, VAT, Remarks, State, StartDate, EndDate, Project_id, CreatedBy_id FROM dbo.Invoice WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT dbo.Tmp_Invoice OFF
GO
ALTER TABLE dbo.InvoiceBooking
	DROP CONSTRAINT FK9D9576249E95258
GO
ALTER TABLE dbo.Correction
	DROP CONSTRAINT FK3232CB7E49E95258
GO
ALTER TABLE dbo.InvoiceLine
	DROP CONSTRAINT FK64ABF92549E95258
GO
DROP TABLE dbo.Invoice
GO
EXECUTE sp_rename N'dbo.Tmp_Invoice', N'Invoice', 'OBJECT' 
GO
ALTER TABLE dbo.Invoice ADD CONSTRAINT
	PK__Invoice__3214EC076C190EBB PRIMARY KEY CLUSTERED 
	(
	Id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE dbo.Invoice ADD CONSTRAINT
	FKDC60817C4985D11 FOREIGN KEY
	(
	Project_id
	) REFERENCES dbo.Project
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.Invoice ADD CONSTRAINT
	FKDC60817FEFE956F FOREIGN KEY
	(
	CreatedBy_id
	) REFERENCES dbo.[User]
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.InvoiceLine ADD CONSTRAINT
	FK64ABF92549E95258 FOREIGN KEY
	(
	Invoice_id
	) REFERENCES dbo.Invoice
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.InvoiceLine SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
BEGIN TRANSACTION
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
ALTER TABLE dbo.Correction SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.InvoiceBooking ADD CONSTRAINT
	FK9D9576249E95258 FOREIGN KEY
	(
	Invoice_id
	) REFERENCES dbo.Invoice
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.InvoiceBooking SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
