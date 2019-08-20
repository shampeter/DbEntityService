SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

USE [master]
GO

IF DB_ID('DbEntityServiceTestDb') IS NULL
BEGIN
    CREATE DATABASE [DbEntityServiceTestDb]
END
GO

IF NOT EXISTS(SELECT * FROM sys.server_principals WHERE NAME = 'DbEntityService')
CREATE LOGIN [DbEntityService] WITH PASSWORD=N'Password1', DEFAULT_DATABASE=[DbEntityServiceTestDb]
go


USE [DbEntityServiceTestDb]
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_sequences]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_sequences]
    (
        [sequence_type] [int] NOT NULL,
        [description] [nvarchar](50) NOT NULL,
        [next_sequence] [int] NOT NULL,
        CONSTRAINT [pk_sequences] PRIMARY KEY ( [sequence_type] )
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_long_sequences]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_long_sequences]
    (
        [long_sequence_type] [int] NOT NULL,
        [description] [nvarchar](50) NOT NULL,
        [next_sequence] [bigint] NOT NULL,
        CONSTRAINT [pk_long_sequences] PRIMARY KEY ( [long_sequence_type] )
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_lookups_group]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_lookups_group]
    (
        [lookups_group_pkey] [int] NOT NULL,
        [description] [nvarchar](50) NOT NULL,
        [version] [rowversion] NOT NULL,
        CONSTRAINT [pk_lookups_group] PRIMARY KEY ( [lookups_group_pkey] ) 
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_lookups]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_lookups]
    (   
        [lookups_pkey] [int] NOT NULL,
        [lookups_group_pkey] [int] NOT NULL,
        [description] [nvarchar](50) NOT NULL,
        [version] [rowversion] NOT NULL,
        CONSTRAINT [pk_lookups] PRIMARY KEY ( [lookups_pkey] ),
        CONSTRAINT [fk_lookups_group] FOREIGN KEY ( [lookups_group_pkey] ) REFERENCES [dbo].[t_lookups_group] ( [lookups_group_pkey] )
    )    
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_company]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_company]
    (   
        [company_pkey] [int] IDENTITY(1,1) NOT NULL,
        [company_name] [nvarchar](50) NOT NULL,
        [company_type_fkey] [int] NOT NULL,
        [added_dt] [datetime] NOT NULL DEFAULT GetDate(),
        [modify_dt] [datetime] NOT NULL DEFAULT GetDate(),
        [added_by] [nvarchar](50) NOT NULL DEFAULT system_user,
        [modify_by] [nvarchar](50) NOT NULL DEFAULT system_user,
        [version] [int] NOT NULL DEFAULT 1,
        CONSTRAINT [pk_company] PRIMARY KEY ( [company_pkey] ),
        CONSTRAINT [fk_company_type] FOREIGN KEY ( [company_type_fkey] ) REFERENCES [dbo].[t_lookups] ( [lookups_pkey] )
    )    
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_ceded_contract]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_ceded_contract]
    (   
        [ceded_contract_pkey] [int] IDENTITY(1,1) NOT NULL,
        [ceded_contract_num] [int] NOT NULL,
        [uw_year] [int] NOT NULL,
        [creation_date] [datetime] NOT NULL,
        [cedant_company_fkey] [int] NOT NULL,
        [xl_company_fkey] [int] NOT NULL,
        [added_dt] [datetime] NOT NULL DEFAULT GetDate(),
        [modify_dt] [datetime] NOT NULL DEFAULT GetDate(),
        [added_by] [nvarchar](50) NOT NULL DEFAULT system_user,
        [modify_by] [nvarchar](50) NOT NULL DEFAULT system_user,
        [version] [rowversion] NOT NULL,
        CONSTRAINT [pk_ceded_contract] PRIMARY KEY ( [ceded_contract_pkey] ),
        CONSTRAINT [fk_xl_company] FOREIGN KEY ( [xl_company_fkey] ) REFERENCES [dbo].[t_company] ( [company_pkey] ),
        CONSTRAINT [fk_cedant_company] FOREIGN KEY ( [cedant_company_fkey] ) REFERENCES [dbo].[t_company] ( [company_pkey] )
    )    
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[t_ceded_contract_layer]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[t_ceded_contract_layer]
    (   
        [ceded_contract_layer_pkey] [int] IDENTITY(1,1) NOT NULL,
        [ceded_contract_fkey] [int] NOT NULL,
        [description] [nvarchar](100) NULL,
        [attachment_point] [money] NOT NULL,
        [layer_type_fkey] int NOT NULL,
        [limit] [money] NOT NULL,
        [added_dt] [datetime] NOT NULL DEFAULT GetDate(),
        [modify_dt] [datetime] NOT NULL DEFAULT GetDate(),
        [added_by] [nvarchar](50) NOT NULL DEFAULT system_user,
        [modify_by] [nvarchar](50) NOT NULL DEFAULT system_user,
        [version] [rowversion] NOT NULL,
        CONSTRAINT [pk_ceded_contract_layer] PRIMARY KEY ( [ceded_contract_layer_pkey] ),
        CONSTRAINT [fk_ceded_contract] FOREIGN KEY ( [ceded_contract_fkey] ) REFERENCES [dbo].[t_ceded_contract] ( [ceded_contract_pkey] ),
        CONSTRAINT [fk_layer_type] FOREIGN KEY ( [layer_type_fkey] ) REFERENCES [dbo].[t_lookups] ( [lookups_pkey] )
    )    
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_sequences] WHERE [sequence_type] = 1
)
BEGIN
    INSERT INTO [dbo].[t_sequences] (
        [sequence_type], [next_sequence], [description]
    ) VALUES (
        1, 1002, 'Ceded Contract Number'
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_long_sequences] WHERE [long_sequence_type] = 1
)
BEGIN
    INSERT INTO [dbo].[t_long_sequences] (
        [long_sequence_type], [next_sequence], [description]
    ) VALUES (
        1, 1000, 'Analysis Number'
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_lookups_group] WHERE [lookups_group_pkey] = 1
)
BEGIN
    INSERT INTO [dbo].[t_lookups_group] (
        [lookups_group_pkey], [description]
    ) VALUES (
        1, 'Company Type'
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_lookups_group] WHERE [lookups_group_pkey] = 2
)
BEGIN
    INSERT INTO [dbo].[t_lookups_group] (
        [lookups_group_pkey], [description]
    ) VALUES (
        2, 'Ceded Contract Type'
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_lookups] WHERE [lookups_group_pkey] = 1
)
BEGIN
    INSERT INTO [dbo].[t_lookups] (
        [lookups_group_pkey], [lookups_pkey], [description]
    ) VALUES (
        1, 10, 'XL Company'
    )
    INSERT INTO [dbo].[t_lookups] (
        [lookups_group_pkey], [lookups_pkey], [description]
    ) VALUES (
        1, 20, 'Cedant Company'
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_lookups] WHERE [lookups_group_pkey] = 2
)
BEGIN
    INSERT INTO [dbo].[t_lookups] (
        [lookups_group_pkey], [lookups_pkey], [description]
    ) VALUES (
        2, 30, 'Excess of Loss'
    )
    INSERT INTO [dbo].[t_lookups] (
        [lookups_group_pkey], [lookups_pkey], [description]
    ) VALUES (
        2, 40, 'Quota Share'
    )
    INSERT INTO [dbo].[t_lookups] (
        [lookups_group_pkey], [lookups_pkey], [description]
    ) VALUES (
        2, 50, 'Stop Loss'
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_company] WHERE [company_name] = 'XL Re America'
)
BEGIN
    INSERT INTO [dbo].[t_company] (
        [company_name], [company_type_fkey]
    ) VALUES (
        'XL Re America', 10
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_company] WHERE [company_name] = 'XL Re Bermuda'
)
BEGIN
    INSERT INTO [dbo].[t_company] (
        [company_name], [company_type_fkey]
    ) VALUES (
        'XL Re Bermuda', 10
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_company] WHERE [company_name] = 'Aon Re Bermuda'
)
BEGIN
    INSERT INTO [dbo].[t_company] (
        [company_name], [company_type_fkey]
    ) VALUES (
        'Aon Re Bermuda', 20
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_company] WHERE [company_name] = 'State Farm'
)
BEGIN
    INSERT INTO [dbo].[t_company] (
        [company_name], [company_type_fkey]
    ) VALUES (
        'State Farm', 20
    )
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_ceded_contract] WHERE [ceded_contract_num] = 1000
)
BEGIN
INSERT INTO [dbo].[t_ceded_contract] (
    [ceded_contract_num], [uw_year], [creation_date], [cedant_company_fkey], [xl_company_fkey]
)
SELECT 
    1000 as [ceded_contract_num],
    2010 as [uw_year],
    '2010-01-03' as [creation_date],
    (SELECT [company_pkey] FROM [dbo].[t_company] WHERE [company_name] = 'Aon Re Bermuda') as [cedant_company_fkey],
    (SELECT [company_pkey] FROM [dbo].[t_company] WHERE [company_name] = 'XL Re America') as [xl_company_fkey]
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_ceded_contract] WHERE [ceded_contract_num] = 1001
)
BEGIN
INSERT INTO [dbo].[t_ceded_contract] (
    [ceded_contract_num], [uw_year], [creation_date], [cedant_company_fkey], [xl_company_fkey]
)
SELECT 
    1001 as [ceded_contract_num],
    2011 as [uw_year],
    '2011-01-10' as [creation_date],
    (SELECT [company_pkey] FROM [dbo].[t_company] WHERE [company_name] = 'State Farm') as [cedant_company_fkey],
    (SELECT [company_pkey] FROM [dbo].[t_company] WHERE [company_name] = 'XL Re Bermuda') as [xl_company_fkey]
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_ceded_contract_layer] as l
    INNER JOIN [dbo].[t_ceded_contract] as c on c.[ceded_contract_pkey] = l.[ceded_contract_fkey]
    WHERE c.ceded_contract_num = 1000 and l.[description] = '300 xs 100'
)
BEGIN
INSERT INTO [dbo].[t_ceded_contract_layer] (
    [ceded_contract_fkey], [description], [attachment_point], [limit], [layer_type_fkey]
)
SELECT 
    (SELECT ceded_contract_pkey FROM t_ceded_contract WHERE ceded_contract_num = 1000) as [ceded_contract_fkey],
    '300 xs 100' as [description],
    100000000 as [attachment_point],
    300000000 as [limit],
    (SELECT lookups_pkey FROM t_lookups WHERE [description] = 'Excess of Loss') as [layer_type_fkey]
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_ceded_contract_layer] as l
    INNER JOIN [dbo].[t_ceded_contract] as c on c.[ceded_contract_pkey] = l.[ceded_contract_fkey]
    WHERE c.ceded_contract_num = 1000 and l.[description] = '< 300'
)
BEGIN
INSERT INTO [dbo].[t_ceded_contract_layer] (
    [ceded_contract_fkey], [description], [attachment_point], [limit], [layer_type_fkey]
)
SELECT 
    (SELECT ceded_contract_pkey FROM t_ceded_contract WHERE ceded_contract_num = 1000) as [ceded_contract_fkey],
    '< 300' as [description],
    -1 as [attachment_point],
    300000000 as [limit],
    (SELECT lookups_pkey FROM t_lookups WHERE [description] = 'Stop Loss') as [layer_type_fkey]
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_ceded_contract_layer] as l
    INNER JOIN [dbo].[t_ceded_contract] as c on c.[ceded_contract_pkey] = l.[ceded_contract_fkey]
    WHERE c.ceded_contract_num = 1001 and l.[description] = '500 xs 300'
)
BEGIN
INSERT INTO [dbo].[t_ceded_contract_layer] (
    [ceded_contract_fkey], [description], [attachment_point], [limit], [layer_type_fkey]
)
SELECT 
    (SELECT ceded_contract_pkey FROM t_ceded_contract WHERE ceded_contract_num = 1001) as [ceded_contract_fkey],
    '500 xs 300' as [description],
    300000000 as [attachment_point],
    500000000 as [limit],
    (SELECT lookups_pkey FROM t_lookups WHERE [description] = 'Excess of Loss') as [layer_type_fkey]
END
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[t_ceded_contract_layer] as l
    INNER JOIN [dbo].[t_ceded_contract] as c on c.[ceded_contract_pkey] = l.[ceded_contract_fkey]
    WHERE c.ceded_contract_num = 1001 and l.[description] = '< 500'
)
BEGIN
INSERT INTO [dbo].[t_ceded_contract_layer] (
    [ceded_contract_fkey], [description], [attachment_point], [limit], [layer_type_fkey]
)
SELECT 
    (SELECT ceded_contract_pkey FROM t_ceded_contract WHERE ceded_contract_num = 1001) as [ceded_contract_fkey],
    '< 500' as [description],
    -1 as [attachment_point],
    500000000 as [limit],
    (SELECT lookups_pkey FROM t_lookups WHERE [description] = 'Stop Loss') as [layer_type_fkey]
END
GO

IF EXISTS (
    SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].[spu_getguid]') AND objectproperty(id, N'IsProcedure') = 1
)
BEGIN
    DROP PROCEDURE [spu_getguid]
END
GO

CREATE PROCEDURE [dbo].[spu_getguid]
    @seq_type int,
    @range int,
    @next_seq int OUTPUT
AS
    DECLARE @Updated table ([next_sequence] int)

    UPDATE [dbo].[t_sequences]
    SET [next_sequence] = [next_sequence] + @range
    OUTPUT deleted.[next_sequence] INTO @Updated
    WHERE [sequence_type] = @seq_type

    SELECT @next_seq = [next_sequence] FROM @Updated
GO

IF EXISTS (
    SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].[spu_getlong]') AND objectproperty(id, N'IsProcedure') = 1
)
BEGIN
    DROP PROCEDURE [spu_getlong]
END
GO

CREATE PROCEDURE [dbo].[spu_getlong]
    @seq_type int,
    @range int,
    @next_seq bigint OUTPUT
AS
    DECLARE @Updated table ([next_sequence] bigint)

    UPDATE [dbo].[t_long_sequences]
    SET [next_sequence] = [next_sequence] + @range
    OUTPUT deleted.[next_sequence] INTO @Updated
    WHERE [long_sequence_type] = @seq_type

    SELECT @next_seq = [next_sequence] FROM @Updated
GO

IF NOT EXISTS(SELECT * FROM sys.database_principals WHERE NAME = 'DbEntityService')
CREATE USER [DbEntityService] FOR LOGIN [DbEntityService] WITH DEFAULT_SCHEMA = [dbo]
GO

ALTER ROLE [db_owner] ADD MEMBER [DbEntityService]
GO