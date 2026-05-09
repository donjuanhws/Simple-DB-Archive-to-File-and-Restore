# Simple DB Archive to File and Restore Console App

A lightweight .NET 8 console application for archiving SQL Server table data to compressed JSON files and restoring archived data back into SQL Server tables.

Supports:

- Batch archiving
- Compressed `.json.gz` storage
- Restore from archive files
- Automatic table creation during restore
- Configurable schema/table mapping
- Large text/JSON column support
- SQL Server bulk insert restore
- Enterprise-style archive packaging

---

# Features

## Archive SQL Server Tables

Archive records from SQL Server tables into compressed JSON files.

Example output:

```text
AuditLogs_20260509120000_1.json.gz
AuditLogs_20260509120000_1.schema.sql
AuditLogs_20260509120000_1.metadata.json
```

---

## Restore Archived Data

Restore archived records into:
- original tables
- different schemas
- different table names
- recovery/staging databases

---

## Automatic Table Creation

If the restore table does not exist:
- the app automatically creates it
- using the archived schema script

---

## Batch Processing

Supports configurable batch sizes for:
- archiving
- restore

Helps with:
- memory usage
- large datasets
- production workloads

---

## Supports Large JSON Columns

Correctly restores:
- `nvarchar(max)`
- serialized JSON payloads
- XML payloads
- Hangfire invocation data
- audit/event payloads

---

# Technology Stack

- .NET 8
- SQL Server
- Dapper
- SqlBulkCopy
- System.Text.Json
- GZip compression

---

# Project Structure

```text
DbArchiver/
│
├── Models/
│   ├── ArchiveSettings.cs
│   └── RestoreSettings.cs
│
├── Services/
│   ├── ArchiveService.cs
│   ├── CompressionService.cs
│   ├── DatabaseService.cs
│   └── RestoreService.cs
│
├── Program.cs
├── appsettings.json
└── DbArchiver.csproj
```

---

# Configuration

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=True;TrustServerCertificate=True"
  },

  "ArchiveSettings": {
    "SchemaName": "dbo",
    "TableName": "AuditLogs",
    "FilterColumn": "CreatedDate",
    "FilterCondition": "<",
    "FilterValue": "2025-01-01",
    "BatchSize": 5000,
    "OutputFolder": "ArchiveOutput",
    "DeleteAfterArchive": false,
    "ExportCreateTableScript": true
  },

  "RestoreSettings": {
    "Enabled": false,
    "SchemaName": "restore",
    "TableName": "AuditLogs_Restore",
    "ArchiveFolder": "ArchiveOutput",
    "FileSearchPattern": "*.json.gz",
    "RestoreBatchSize": 5000,
    "TruncateTableBeforeRestore": false
  }
}
```

---

# Archive Mode

Set:

```json
"Enabled": false
```

in `RestoreSettings`.

The application will:
1. Read records from SQL Server
2. Export records to compressed JSON
3. Export schema script
4. Export metadata
5. Optionally delete archived records

---

# Restore Mode

Set:

```json
"Enabled": true
```

The application will:
1. Read `.json.gz` files
2. Create restore table if needed
3. Bulk insert records into SQL Server

---

# Archive Package Contents

Each archive batch contains:

## Data File

```text
AuditLogs_1.json.gz
```

Compressed JSON data.

---

## Schema File

```text
AuditLogs_1.schema.sql
```

SQL create table script.

---

## Metadata File

```json
{
  "SourceSchema": "dbo",
  "SourceTable": "AuditLogs",
  "RowCount": 5000,
  "CreatedUtc": "2026-05-09T12:00:00"
}
```

---

# Supported Filter Conditions

- `<`
- `>`
- `=`
- `<=`
- `>=`

Example:

```json
"FilterCondition": "<"
```

---

# Example Use Cases

## Audit Log Archiving

Archive old audit records:

```json
"FilterColumn": "CreatedDate",
"FilterCondition": "<",
"FilterValue": "2025-01-01"
```

---

## Hangfire Job Archiving

Archive Hangfire tables containing serialized invocation payloads.

---

## Event Store Archiving

Archive large JSON event payloads safely.

---

# Restore to Different Table

Archive from:

```text
[dbo].[AuditLogs]
```

Restore to:

```text
[restore].[AuditLogs_Restore]
```

without affecting production tables.

---

# Build

## Requirements

- .NET 8 SDK
- SQL Server

---

## Build Command

```bash
dotnet build
```

---

## Run

```bash
dotnet run
```

---

# NuGet Packages

```bash
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.Binder
```

---

# Important Notes

## nvarchar(max) Support

The application correctly restores:
- large JSON payloads
- serialized objects
- large text columns

using proper SQL type reconstruction.

---

## Automatic Table Creation

Restore tables are only created if they do not already exist.

---

## JsonElement Conversion

The restore engine automatically converts:
- `JsonElement`
- numbers
- booleans
- nested JSON

into proper SQL-compatible CLR types before bulk insert.

---

# Current Limitations

The schema generator currently does NOT fully support:

- primary keys
- foreign keys
- indexes
- identity columns
- default constraints
- decimal precision/scale
- computed columns

The solution is intended for:
- lightweight archival
- disaster recovery staging
- data retention
- operational backups

---

# Recommended Future Enhancements

- SHA256 archive verification
- AES encrypted archives
- Azure Blob Storage support
- AWS S3 support
- Parallel archiving
- Incremental watermark archiving
- Identity column preservation
- Full SQL schema fidelity
- Multi-table configuration
- Transactional delete/archive
- Restore validation reports

---

# License

MIT License

---

# Author

Dev Partners ZA Team : Simple DB Archive to File and Restore Console App
Built with .NET 8 and SQL Server.
