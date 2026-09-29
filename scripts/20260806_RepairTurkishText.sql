/*
   UTF-8 SQL dosyasının eski sqlcmd kod sayfasıyla çalıştırılması sonucu oluşan
   mojibake metinlerini veri kaybetmeden onarır. Tekrar çalıştırılabilir.

   Çalıştırma:
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d BakimYonetimiDb -E -b -f 65001 \
     -i .\scripts\20260806_RepairTurkishText.sql
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION dbo.RepairUtf8Mojibake(@value nvarchar(max))
RETURNS nvarchar(max)
AS
BEGIN
    IF @value IS NULL OR
       (CHARINDEX(NCHAR(195), @value) = 0 AND CHARINDEX(NCHAR(196), @value) = 0 AND
        CHARINDEX(NCHAR(197), @value) = 0 AND CHARINDEX(NCHAR(226), @value) = 0)
        RETURN @value;

    DECLARE @legacy varchar(max) =
        CONVERT(varchar(max), @value COLLATE Latin1_General_100_CI_AS);
    DECLARE @decoded table
    (
        Value varchar(max) COLLATE Latin1_General_100_CI_AS_SC_UTF8
    );
    INSERT @decoded(Value) SELECT CONVERT(varbinary(max), @legacy);
    RETURN (SELECT CONVERT(nvarchar(max), Value) FROM @decoded);
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @schema sysname, @table sysname, @column sysname, @sql nvarchar(max);
    DECLARE text_columns CURSOR LOCAL FAST_FORWARD FOR
        SELECT schema_name(t.schema_id), t.name, c.name
        FROM sys.tables t
        INNER JOIN sys.columns c ON c.object_id = t.object_id
        WHERE t.is_ms_shipped = 0 AND c.is_computed = 0
          AND c.system_type_id IN (231, 239);

    OPEN text_columns;
    FETCH NEXT FROM text_columns INTO @schema, @table, @column;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'UPDATE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(@table) +
            N' SET ' + QUOTENAME(@column) + N' = dbo.RepairUtf8Mojibake(' + QUOTENAME(@column) + N')' +
            N' WHERE CHARINDEX(NCHAR(195), ' + QUOTENAME(@column) + N') > 0' +
            N' OR CHARINDEX(NCHAR(196), ' + QUOTENAME(@column) + N') > 0' +
            N' OR CHARINDEX(NCHAR(197), ' + QUOTENAME(@column) + N') > 0' +
            N' OR CHARINDEX(NCHAR(226), ' + QUOTENAME(@column) + N') > 0;';
        EXEC sys.sp_executesql @sql;
        FETCH NEXT FROM text_columns INTO @schema, @table, @column;
    END;
    CLOSE text_columns;
    DEALLOCATE text_columns;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.BakimPlanlari') AND name = 'UQ_BakimPlanlari_AktifHibrit')
        DROP INDEX UQ_BakimPlanlari_AktifHibrit ON dbo.BakimPlanlari;
    CREATE UNIQUE INDEX UQ_BakimPlanlari_AktifHibrit ON dbo.BakimPlanlari(MakineId)
        WHERE AnalizId IS NOT NULL AND Durum IN (N'Taslak', N'Planlandı', N'Onaylandı', N'Devam Ediyor');

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('dbo.MakineCalismaTakvimleri') AND name = 'DF_MakineCalismaTakvimleri_Durum')
        ALTER TABLE dbo.MakineCalismaTakvimleri DROP CONSTRAINT DF_MakineCalismaTakvimleri_Durum;
    ALTER TABLE dbo.MakineCalismaTakvimleri ADD CONSTRAINT DF_MakineCalismaTakvimleri_Durum DEFAULT(N'Planlandı') FOR Durum;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

DROP FUNCTION dbo.RepairUtf8Mojibake;
GO
