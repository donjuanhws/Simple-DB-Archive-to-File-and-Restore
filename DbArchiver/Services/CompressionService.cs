using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace DbArchiver.Services;

public static class CompressionService
{
    public static async Task WriteCompressedJsonAsync<T>(
        string path,
        T data)
    {
        var json = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions
            {
                WriteIndented = false
            });

        await using var fileStream = File.Create(path);

        await using var gzip = new GZipStream(
            fileStream,
            CompressionLevel.Optimal);

        await using var writer = new StreamWriter(
            gzip,
            Encoding.UTF8);

        await writer.WriteAsync(json);
    }
}