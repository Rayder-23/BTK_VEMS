/* Outputs schema lines for Docs/db.txt (dbo user tables only). */
SET NOCOUNT ON;

DECLARE @lines TABLE (sort_key bigint, line nvarchar(4000));

;WITH pk_cols AS (
    SELECT ic.object_id, ic.column_id
    FROM sys.index_columns ic
    INNER JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.is_primary_key = 1
),
tbl AS (
    SELECT t.object_id, t.name AS tname,
           ROW_NUMBER() OVER (ORDER BY t.name) AS table_seq
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.is_ms_shipped = 0 AND t.name <> N'sysdiagrams'
),
def_cols AS (
    SELECT
        dc.parent_object_id,
        dc.parent_column_id,
        dc.definition
    FROM sys.default_constraints dc
),
col_fmt AS (
    SELECT
        t.table_seq,
        t.tname,
        c.column_id,
        N'  - ' + c.name + N': '
        + CASE typ.name
            WHEN N'varchar' THEN N'varchar(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length AS nvarchar(10)) END + N')'
            WHEN N'char' THEN N'char(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length AS nvarchar(10)) END + N')'
            WHEN N'varbinary' THEN N'varbinary(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length AS nvarchar(10)) END + N')'
            WHEN N'binary' THEN N'binary(' + CAST(c.max_length AS nvarchar(10)) + N')'
            WHEN N'nvarchar' THEN N'nvarchar(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length / 2 AS nvarchar(10)) END + N')'
            WHEN N'nchar' THEN N'nchar(' + CAST(c.max_length / 2 AS nvarchar(10)) + N')'
            WHEN N'decimal' THEN N'decimal(' + CAST(c.precision AS nvarchar(10)) + N',' + CAST(c.scale AS nvarchar(10)) + N')'
            WHEN N'numeric' THEN N'numeric(' + CAST(c.precision AS nvarchar(10)) + N',' + CAST(c.scale AS nvarchar(10)) + N')'
            WHEN N'datetime2' THEN N'datetime2(' + CAST(c.scale AS nvarchar(10)) + N')'
            WHEN N'datetimeoffset' THEN N'datetimeoffset(' + CAST(c.scale AS nvarchar(10)) + N')'
            WHEN N'time' THEN N'time(' + CAST(c.scale AS nvarchar(10)) + N')'
            ELSE typ.name
          END
        + CASE WHEN c.is_nullable = 1 THEN N' NULL' ELSE N' NOT NULL' END
        + CASE WHEN pk.object_id IS NOT NULL THEN N' PK' ELSE N'' END
        + CASE WHEN dc.definition IS NOT NULL THEN N' DEFAULT ' + dc.definition ELSE N'' END AS col_line
    FROM tbl t
    INNER JOIN sys.columns c ON c.object_id = t.object_id
    INNER JOIN sys.types typ ON typ.user_type_id = c.user_type_id
    LEFT JOIN pk_cols pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
    LEFT JOIN def_cols dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
)
INSERT INTO @lines (sort_key, line)
SELECT CAST(t.table_seq AS bigint) * 100000 + 0, N'Table: dbo.' + t.tname
FROM tbl t
UNION ALL
SELECT CAST(cf.table_seq AS bigint) * 100000 + cf.column_id, cf.col_line
FROM col_fmt cf;

SELECT line FROM @lines ORDER BY sort_key;
