USE [DbEntityServiceTestDb]
GO

SELECT c.company_pkey, c.company_name, l.[description] as [company_type], c.[version]
FROM [dbo].[t_company] c 
INNER JOIN [dbo].[t_lookups] l on l.lookups_pkey = c.company_type_fkey
GO

-- Select rows from a Table or View '[t_ceded_contract]' in schema '[dbo]'
SELECT 
c.ceded_contract_num, c.uw_year, c.creation_date, c.[version], xl.company_name as [xl_company], ced.company_name as [cedent_name], c.[modify_by], c.[modify_dt],
l.[description], lt.[description] as [layer_type], l.attachment_point, l.limit 
FROM [dbo].[t_ceded_contract] c
INNER JOIN [dbo].[t_ceded_contract_layer] l on l.ceded_contract_fkey = c.ceded_contract_pkey
INNER JOIN [dbo].[t_lookups] lt on lt.lookups_pkey = l.layer_type_fkey
INNER JOIN [dbo].[t_company] xl on xl.company_pkey = c.xl_company_fkey
INNER JOIN [dbo].[t_company] ced on ced.company_pkey = c.cedant_company_fkey
-- WHERE c.[ceded_contract_num] = 1001
GO

SELECT * FROM [dbo].[t_sequences]

DECLARE @next_seq INT

EXEC [dbo].[spu_getguid] 1, 1, @next_seq OUT

SELECT @next_seq

SELECT * FROM [dbo].[t_sequences]

SELECT * FROM [dbo].[t_long_sequences]

DECLARE @next_seq_long bigint

EXEC [dbo].[spu_getlong] 1, 1, @next_seq_long OUT

SELECT @next_seq_long

SELECT * FROM [dbo].[t_long_sequences]