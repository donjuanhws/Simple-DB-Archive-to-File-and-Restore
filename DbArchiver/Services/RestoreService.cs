using DbArchiver.Models;
using System.IO.Compression;
using System.Text.Json;

namespace DbArchiver.Services;

public class RestoreService
{
    private readonly DatabaseService _dbService;

    public RestoreService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task RunAsync(
        RestoreSettings settings)
    {
        if (!Directory.Exists(settings.ArchiveFolder))
        {
            Console.WriteLine("Archive folder not found.");
            return;
        }

        if (settings.TruncateTableBeforeRestore)
        {
            await _dbService.TruncateTableAsync(
                settings.SchemaName,
                settings.TableName);
        }

        var files = Directory.GetFiles(
            settings.ArchiveFolder,
            settings.FileSearchPattern);

        foreach (var file in files)
        {
            var exists =
                await _dbService.TableExistsAsync(
                    settings.SchemaName,
                    settings.TableName);

            if (!exists)
            {
                var schemaFile =
                    file.Replace(".json.gz", ".schema.sql");

                if (!File.Exists(schemaFile))
                {
                    throw new Exception(
                        $"Schema file missing: {schemaFile}");
                }

                var schemaSql =
                    await File.ReadAllTextAsync(schemaFile);

                await _dbService.ExecuteSqlAsync(schemaSql);
            }

            var rows =
                await ReadCompressedJsonAsync(file);

            await _dbService.BulkInsertAsync(
                settings.SchemaName,
                settings.TableName,
                rows);
        }
    }

    private static async Task<List<dynamic>>
        ReadCompressedJsonAsync(string path)
    {
        await using var file =
            File.OpenRead(path);

        await using var gzip =
            new GZipStream(
                file,
                CompressionMode.Decompress);

        using var reader =
            new StreamReader(gzip);

        var json =
            await reader.ReadToEndAsync();

        var rows =
            JsonSerializer.Deserialize<
                List<Dictionary<string, object>>
            >(json);

        return rows?.Cast<dynamic>().ToList()
               ?? new List<dynamic>();
    }
}