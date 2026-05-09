namespace DbArchiver.Models;

public class ArchiveSettings
{
    public string SchemaName { get; set; } = "dbo";

    public string TableName { get; set; } = string.Empty;

    public string FilterColumn { get; set; } = string.Empty;

    public string FilterCondition { get; set; } = "<";

    public string FilterValue { get; set; } = string.Empty;

    public int BatchSize { get; set; } = 1000;

    public string OutputFolder { get; set; } = "ArchiveOutput";

    public bool DeleteAfterArchive { get; set; }

    public bool ExportCreateTableScript { get; set; } = true;
}