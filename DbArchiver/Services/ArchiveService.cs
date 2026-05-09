using DbArchiver.Models;
using System.Text.Json;

namespace DbArchiver.Services;

public class ArchiveService
{
    private readonly DatabaseService _dbService;

    public ArchiveService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task RunAsync(ArchiveSettings settings, RestoreSettings restore)
    {
        Directory.CreateDirectory(settings.OutputFolder);

        int batchNumber = 1;

        while (true)
        {
            var rows = await _dbService.GetBatchAsync(
                settings.SchemaName,
                settings.TableName,
                settings.FilterColumn,
                settings.FilterCondition,
                settings.FilterValue,
                settings.BatchSize);

            if (rows.Count == 0)
            {
                Console.WriteLine("No more records.");
                break;
            }

            var baseFileName =
                $"{settings.TableName}_{DateTime.UtcNow:yyyyMMddHHmmss}_{batchNumber}";

            var archivePath =
                Path.Combine(
                    settings.OutputFolder,
                    $"{baseFileName}.json.gz");

            await CompressionService.WriteCompressedJsonAsync(
                archivePath,
                rows);

            if (settings.ExportCreateTableScript)
            {
                var createScript =
                    await _dbService.GenerateCreateTableScriptAsync(
                        settings.SchemaName,
                        settings.TableName,
                        restore.SchemaName,
                        restore.TableName);

                var schemaFile =
                    Path.Combine(
                        settings.OutputFolder,
                        $"{baseFileName}.schema.sql");

                await File.WriteAllTextAsync(
                    schemaFile,
                    createScript);
            }

            var metadata = new
            {
                SourceSchema = settings.SchemaName,
                SourceTable = settings.TableName,
                RowCount = rows.Count,
                CreatedUtc = DateTime.UtcNow
            };

            var metadataFile =
                Path.Combine(
                    settings.OutputFolder,
                    $"{baseFileName}.metadata.json");

            await File.WriteAllTextAsync(
                metadataFile,
                JsonSerializer.Serialize(
                    metadata,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

            if (settings.DeleteAfterArchive)
            {
                await _dbService.DeleteBatchAsync(
                    settings.SchemaName,
                    settings.TableName,
                    settings.FilterColumn,
                    settings.FilterCondition,
                    settings.FilterValue,
                    settings.BatchSize);
            }

            batchNumber++;
        }
    }
}