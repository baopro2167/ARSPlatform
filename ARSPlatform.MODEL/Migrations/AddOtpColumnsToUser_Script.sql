-- Add OTP columns to User table
-- Run this script on your SQL Server database

-- Check if columns exist before adding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.User') AND name = 'OtpCode')
BEGIN
    ALTER TABLE [dbo].[User] ADD [OtpCode] NVARCHAR(MAX) NULL;
    PRINT 'Added OtpCode column';
END
ELSE
    PRINT 'OtpCode column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.User') AND name = 'ExpiresOtpAt')
BEGIN
    ALTER TABLE [dbo].[User] ADD [ExpiresOtpAt] DATETIME2 NULL;
    PRINT 'Added ExpiresOtpAt column';
END
ELSE
    PRINT 'ExpiresOtpAt column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.User') AND name = 'IsOtpUsed')
BEGIN
    ALTER TABLE [dbo].[User] ADD [IsOtpUsed] BIT NULL;
    PRINT 'Added IsOtpUsed column';
END
ELSE
    PRINT 'IsOtpUsed column already exists';

PRINT 'OTP columns migration completed successfully!';
