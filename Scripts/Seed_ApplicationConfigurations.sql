/* Seeds Configurations keys for StudentApplications (idempotent). */
SET NOCOUNT ON;

DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'ApplicationStatuses')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValues, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'ApplicationStatuses', N'Pending,UnderReview,Approved,Rejected,Converted As Student', N'Student application workflow statuses', 1, @now, @now);

UPDATE dbo.Configurations
SET ConfigValues = ConfigValues + N',Converted As Student',
    UpdatedAt = SYSUTCDATETIME()
WHERE ConfigKey = N'ApplicationStatuses'
  AND ConfigValues NOT LIKE N'%Converted As Student%';

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'ApplicationSourceChannels')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValues, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'ApplicationSourceChannels', N'Online,WalkIn,Phone,Referral', N'How the application was received', 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'ApplicationTestStatuses')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValues, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'ApplicationTestStatuses', N'NotScheduled,Scheduled,Pass,Fail', N'Admission test tracking', 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'ApplicationPaymentStatuses')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValues, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'ApplicationPaymentStatuses', N'Pending,Paid,Partial,Unpaid', N'Application fee payment state', 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM dbo.Configurations WHERE ConfigKey = N'Genders')
    INSERT INTO dbo.Configurations (ConfigKey, ConfigValues, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'Genders', N'M,F,O', N'Gender codes for profiles and applications', 1, @now, @now);
