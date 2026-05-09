using DbArchiver.Models;
using DbArchiver.Services;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var archiveSettings = configuration
    .GetSection("ArchiveSettings")
    .Get<ArchiveSettings>();

var restoreSettings = configuration
    .GetSection("RestoreSettings")
    .Get<RestoreSettings>();

var connectionString =
    configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Connection string missing.");
    return;
}

var dbService = new DatabaseService(connectionString);

if (restoreSettings != null && restoreSettings.Enabled)
{
    var restoreService = new RestoreService(dbService);
    await restoreService.RunAsync(restoreSettings);
}
else if (archiveSettings != null)
{
    var archiveService = new ArchiveService(dbService);
    await archiveService.RunAsync(archiveSettings, restoreSettings);
}
else
{
    Console.WriteLine("No configuration found.");
}

Console.WriteLine("Done.");