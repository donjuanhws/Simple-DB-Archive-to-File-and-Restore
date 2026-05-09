namespace DbArchiver.Models;

public class RestoreSettings
{
    public bool Enabled { get; set; }

    public string SchemaName { get; set; } = "dbo";

    public string TableName { get; set; } = string.Empty;

    public string ArchiveFolder { get; set; } = "ArchiveOutput";

    public string FileSearchPattern { get; set; } = "*.json.gz";

    public int RestoreBatchSize { get; set; } = 5000;

    public bool TruncateTableBeforeRestore { get; set; }
}