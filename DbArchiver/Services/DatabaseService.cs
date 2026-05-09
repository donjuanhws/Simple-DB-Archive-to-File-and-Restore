using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace DbArchiver.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    private static readonly HashSet<string> AllowedConditions =
    [
        "<",
        ">",
        "=",
        "<=",
        ">="
    ];

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private static string ValidateCondition(string condition)
    {
        if (!AllowedConditions.Contains(condition))
        {
            throw new Exception($"Invalid filter condition: {condition}");
        }

        return condition;
    }

    private static string GetFullTableName(
        string schema,
        string table)
    {
        return $"[{schema}].[{table}]";
    }

    public async Task<List<dynamic>> GetBatchAsync(
        string schema,
        string table,
        string filterColumn,
        string filterCondition,
        string filterValue,
        int batchSize)
    {
        filterCondition = ValidateCondition(filterCondition);

        var fullTableName = GetFullTableName(schema, table);

        var sql = $@"
SELECT TOP (@BatchSize) *
FROM {fullTableName}
WHERE [{filterColumn}] {filterCondition} @FilterValue
ORDER BY [{filterColumn}]";

        await using var conn = new SqlConnection(_connectionString);

        var result = await conn.QueryAsync(
            sql,
            new
            {
                BatchSize = batchSize,
                FilterValue = filterValue
            });

        return result.ToList();
    }

    public async Task DeleteBatchAsync(
        string schema,
        string table,
        string filterColumn,
        string filterCondition,
        string filterValue,
        int batchSize)
    {
        filterCondition = ValidateCondition(filterCondition);

        var fullTableName = GetFullTableName(schema, table);

        var sql = $@"
DELETE TOP (@BatchSize)
FROM {fullTableName}
WHERE [{filterColumn}] {filterCondition} @FilterValue";

        await using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync(
            sql,
            new
            {
                BatchSize = batchSize,
                FilterValue = filterValue
            });
    }

    public async Task<bool> TableExistsAsync(
        string schema,
        string table)
    {
        var sql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = @Schema
AND TABLE_NAME = @Table";

        await using var conn =
            new SqlConnection(_connectionString);

        var count = await conn.ExecuteScalarAsync<int>(
            sql,
            new
            {
                Schema = schema,
                Table = table
            });

        return count > 0;
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await using var conn =
            new SqlConnection(_connectionString);

        await conn.ExecuteAsync(sql);
    }

    public async Task<string> GenerateCreateTableScriptAsync(
        string sourceSchema,
        string sourceTable,
        string targetSchema,
        string targetTable)
    {
        var sql = @"
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @Schema
AND TABLE_NAME = @Table
ORDER BY ORDINAL_POSITION";

        await using var conn =
            new SqlConnection(_connectionString);

        var columns = await conn.QueryAsync(
            sql,
            new
            {
                Schema = sourceSchema,
                Table = sourceTable
            });

        var sb = new StringBuilder();

        sb.AppendLine($@"
IF NOT EXISTS
(
    SELECT 1
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = '{targetSchema}'
    AND TABLE_NAME = '{targetTable}'
)
BEGIN

CREATE TABLE [{targetSchema}].[{targetTable}]
(");

        var lines = new List<string>();

        foreach (IDictionary<string, object> col in columns)
        {
            var columnName =
                col["COLUMN_NAME"]?.ToString();

            var dataType =
                col["DATA_TYPE"]?.ToString();

            var maxLength =
                col["CHARACTER_MAXIMUM_LENGTH"];

            var isNullable =
                col["IS_NULLABLE"]?.ToString();

            string typeDefinition;

            if (maxLength != null &&
                maxLength != DBNull.Value)
            {
                var length = Convert.ToInt32(maxLength);

                if (length == -1)
                {
                    typeDefinition =
                        $"{dataType}(MAX)";
                }
                else if (length > 0)
                {
                    typeDefinition =
                        $"{dataType}({length})";
                }
                else
                {
                    typeDefinition = dataType!;
                }
            }
            else
            {
                typeDefinition = dataType!;
            }

            var nullable =
                isNullable == "YES"
                    ? "NULL"
                    : "NOT NULL";

            lines.Add(
                $"    [{columnName}] {typeDefinition} {nullable}");
        }

        sb.AppendLine(
            string.Join("," + Environment.NewLine, lines));

        sb.AppendLine(")");

        sb.AppendLine("END");

        return sb.ToString();
    }

    public async Task TruncateTableAsync(
        string schema,
        string table)
    {
        var fullTableName =
            GetFullTableName(schema, table);

        var sql =
            $"TRUNCATE TABLE {fullTableName}";

        await using var conn =
            new SqlConnection(_connectionString);

        await conn.ExecuteAsync(sql);
    }

    public async Task BulkInsertAsync(
        string schema,
        string table,
        List<dynamic> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var fullTableName =
            GetFullTableName(schema, table);

        var dataTable =
            ConvertToDataTable(rows);

        await using var conn =
            new SqlConnection(_connectionString);

        await conn.OpenAsync();

        using var bulkCopy =
            new SqlBulkCopy(conn)
            {
                DestinationTableName = fullTableName,
                BatchSize = 5000
            };

        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(
                column.ColumnName,
                column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable);
    }

    private static DataTable ConvertToDataTable(
        List<dynamic> rows)
    {
        var table = new DataTable();

        IDictionary<string, object> firstRow =
            (IDictionary<string, object>)rows[0];

        foreach (var key in firstRow.Keys)
        {
            table.Columns.Add(key);
        }

        foreach (IDictionary<string, object> row in rows)
        {
            var values = row.Values
                .Select(ConvertJsonValue)
                .ToArray();

            table.Rows.Add(values);
        }

        return table;
    }

    private static object ConvertJsonValue(object value)
    {
        if (value == null)
        {
            return DBNull.Value;
        }

        if (value is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString()
                           ?? (object)DBNull.Value;

                case JsonValueKind.Number:

                    if (jsonElement.TryGetInt32(out var intValue))
                        return intValue;

                    if (jsonElement.TryGetInt64(out var longValue))
                        return longValue;

                    if (jsonElement.TryGetDecimal(out var decimalValue))
                        return decimalValue;

                    return jsonElement.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return jsonElement.GetBoolean();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return DBNull.Value;

                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return jsonElement.GetRawText();

                default:
                    return jsonElement.ToString();
            }
        }

        return value;
    }
}