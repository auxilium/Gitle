/*
   dinsdag 25 april 201709:23:49
   User: 
   Server: statler2016
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
ALTER TABLE dbo.PlanningItem ADD
	Text nvarchar(255) NULL
GO
ALTER TABLE dbo.PlanningItem SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
