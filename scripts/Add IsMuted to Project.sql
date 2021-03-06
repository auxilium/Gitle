/*
   woensdag 10 oktober 201810:58:49
   User: 
   Server: STATLER2016
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
ALTER TABLE dbo.Project ADD
	IsMuted bit NULL
GO
ALTER TABLE dbo.Project SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
