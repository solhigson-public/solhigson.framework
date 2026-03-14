# Archive Pattern

## Contents

- [Partition-Based Archive Pattern](#partition-based-archive-pattern)
  - [When to Apply](#when-to-apply)
  - [Entity Layer](#entity-layer)
  - [DbContext Registration](#dbcontext-registration)
  - [Migration](#migration)
  - [Metadata: ArchiveInfo](#metadata-archiveinfo)
  - [Query Layer: ArchiveService](#query-layer-archiveservice)
  - [Execution: Hangfire Jobs](#execution-hangfire-jobs)
  - [Shared Utilities](#shared-utilities)
  - [Settings](#settings)
  - [Timezone Handling](#timezone-handling)

---

## Partition-Based Archive Pattern

For high-volume tables that need data lifecycle management — move old data from the active table to an archive table using SQL Server partitioning, with optional export to a data warehouse.

### When to Apply

- Tables expected to grow beyond millions of rows
- Tables with time-series access patterns (queries almost always include a date range)
- Tables where old data is rarely accessed but must be queryable

### Entity Layer

#### Interface — shared contract for entity + DTOs

```csharp
public interface I{Entity}
{
    // All queryable/filterable properties
}
```

#### Base class — all properties, indexes, annotations

```csharp
[Index(nameof(Created))]
public abstract record {Entity}Base : EntityBase, I{Entity}
{
    // All properties and data annotations live here
    // Indexes defined as attributes represent LOGICAL indexes
    // For partitioned tables, the migration is the source of truth for physical index layout
}
```

#### Active entity (standard code generation)

```csharp
public record {Entity} : {Entity}Base { }
```

#### Archive entity (excluded from code generation)

```csharp
[Table("{Entity}_Archive")]
public record {Entity}Archive : {Entity}Base, IEfCoreGenIgnore { }
```

- `IEfCoreGenIgnore` prevents `Solhigson.Framework.efcoretool` from generating repositories/DTOs for the archive entity
- Both map to identically-structured tables

#### Export ledger — single consolidated table

One shared `ExportLedger` table tracks all archivable entities. No per-entity ExportLedger tables.

```csharp
[Index(nameof(TableName), nameof(DayStartUtc), nameof(DayEndUtc), IsUnique = true)]
[Index(nameof(DayEndUtc))]
public record ExportLedger : EntityBase, IEfCoreGenIgnore
{
    [Required]
    [StringLength(200)]
    [Column(TypeName = "VARCHAR")]
    public string TableName { get; set; }

    public DateTime DayStartUtc { get; set; }
    public DateTime DayEndUtc { get; set; }
    public DateTime ExportedAtUtc { get; set; }
    public long RowCount { get; set; }
}
```

- Unique index on `(TableName, DayStartUtc, DayEndUtc)` prevents duplicate exports per table per day
- `TableName` matches the active table name (e.g. `"{Entities}"`)
- Hangfire jobs filter by `TableName` when querying/pruning ledger rows

### DbContext Registration

```csharp
// Per archivable entity
public DbSet<{Entity}> {Entities} { get; set; }
public DbSet<{Entity}Archive> {Entities}Archive { get; set; }

// Shared — one of each for all entities
public DbSet<ExportLedger> ExportLedgers { get; set; }
public DbSet<ArchiveInfo> ArchiveInfo { get; set; }
```

### Migration

Create both tables with identical schema. Set up partition function + scheme on `Created` column from day one.

#### Partitioning setup

```sql
CREATE PARTITION FUNCTION PF_{Entity}_CreatedDay (datetime2(7))
AS RANGE RIGHT FOR VALUES (/* initial daily boundary values */);

CREATE PARTITION SCHEME PS_{Entity}_CreatedDay
AS PARTITION PF_{Entity}_CreatedDay ALL TO ([PRIMARY]);
```

#### Index requirements for partitioned tables

Entity `[Index]` attributes define **logical** indexes for readability. For partitioned tables, the **migration is the source of truth** for physical index layout:

1. **Clustered PK** — `(Id, Created)` ON the partition scheme (not just `Id`)
2. **All unique indexes** — MUST include `Created` (SQL Server requirement), ON the partition scheme
3. **All non-clustered indexes** — MUST be aligned to the partition scheme (required for partition switching)
4. **Archive table foreign keys** — drop all FKs on the archive table (enables partition switching, avoids overhead on cold data)

Active table retains its foreign keys normally.

### Metadata: ArchiveInfo

One `ArchiveInfo` row per archivable table, tracking:

| Property | Purpose |
|----------|---------|
| `Table` (unique) | Table name being archived |
| `Status` | `Started` → `TableReInitialized` → `DataMigrated` |
| `RowsArchived` | Running count of rows moved |
| `NextArchiveDate` | When next archive is scheduled |
| `ArchiveStartTime` | When current archive cycle began |
| Per-step fields | `IsPartitionSwitchSuccessful`, `IsExportToStorageSuccessful`, `IsArchiveDataPruneSuccessful` + error/time for each |

Helper methods on ArchiveInfo:
- `SetPartitionArchivingStarted()` — clears row count, sets start time
- `SetPartitionArchiveFinished(ArchiveStep, DateTime, string?)` — records success/failure + duration per step

### Query Layer: ArchiveService

Smart date-aware querying that transparently searches active table, archive table, and data warehouse.

#### Data lifecycle and query order

| Tier | Source | Data age | Query method |
|------|--------|----------|--------------|
| Hot | Active table | Last N days (`KeepDaysInRealtime`) | EF Core LINQ |
| Cold | Archive table | Up to `ArchiveRetentionMonths` | EF Core LINQ |
| Historical | Cloud storage (Parquet) | Beyond archive retention | Serverless SQL endpoint |

#### Date-aware decision logic

```csharp
async Task<(bool CheckArchive, bool CheckWarehouse)> ShouldCheckSourcesAsync(
    bool? checkArchive, PagedRequest? request)
{
    if (checkArchive.HasValue)
        return (checkArchive.Value, checkArchive.Value);

    if (request?.StartDate is null)
        return (true, false); // no date range — check archive, skip warehouse

    var archiveInfo = await GetArchiveInfoAsync<{Entity}>();
    var tableSettings = GetTableSettings();

    var archivePruneCutoff = DateTime.UtcNow
        .AddMonths(-tableSettings.ArchiveRetentionMonths);

    var shouldCheckArchive = request.StartDate <= archiveInfo?.ArchiveStartTime;
    var shouldCheckWarehouse = request.StartDate <= archivePruneCutoff;

    return (shouldCheckArchive, shouldCheckWarehouse);
}
```

- `request.StartDate > archiveStartTime` → active only
- `request.StartDate > archivePruneCutoff` → active + archive
- `request.StartDate <= archivePruneCutoff` → active + archive + data warehouse

#### Single-record lookups

Sequential fallback — active → archive → data warehouse:

```csharp
public async Task<T?> Get{Entity}ByIdAsync<T>(string id)
{
    // 1. Active table
    var result = await DbContext.Set<{Entity}>()
        .AsNoTracking().Where(x => x.Id == id)
        .ProjectToType<T>().FirstOrDefaultAsync();

    if (result is not null) return result;

    // 2. Archive table
    result = await DbContext.Set<{Entity}Archive>()
        .AsNoTracking().Where(x => x.Id == id)
        .ProjectToType<T>().FirstOrDefaultAsync();

    if (result is not null) return result;

    // 3. Data warehouse (serverless SQL over Parquet)
    result = await QueryWarehouseByIdAsync<T>(id);

    return result;
}
```

#### List queries

Active + archive concat in SQL; data warehouse results merged in memory:

```csharp
var activeQuery = DbContext.Set<{Entity}>()
    .AsNoTracking().Where(predicate)
    .OrderByDescending(x => x.Created).Take(pageSize);

if (shouldCheckArchive)
{
    var archiveQuery = DbContext.Set<{Entity}Archive>()
        .AsNoTracking().Where(predicate)
        .OrderByDescending(x => x.Created).Take(pageSize);

    activeQuery = activeQuery.Concat(archiveQuery);
}

var results = await activeQuery.ProjectToType<T>().ToListAsync();

if (shouldCheckWarehouse)
{
    var warehouseResults = await QueryWarehouseAsync<T>(predicate, pageSize);
    results = results.Concat(warehouseResults)
        .OrderByDescending(x => x.Created)
        .Take(pageSize)
        .ToList();
}
```

#### Generic method signature

```csharp
GetQueryAsync<TResult, TActive, TArchive, TBase>(
    Expression<Func<TBase, bool>> predicate,
    bool? checkArchive,
    PagedRequest? request,
    bool readFromPrimary = false)
```

Reusable for any entity pair that follows this pattern.

### Execution: Hangfire Jobs

#### Generic base class

All partition archive logic lives in a reusable abstract base. Concrete jobs provide table-specific configuration:

```csharp
public abstract class PartitionArchiveJobBase<TEntity, TArchive>(
    ServicesWrapper servicesWrapper,
    IRepositoryWrapper repositoryWrapper,
    PartitionArchiveSettings archiveSettings,
    IStorageService storageService) : AsyncJobBase(servicesWrapper)
    where TEntity : EntityBase
    where TArchive : EntityBase
{
    protected abstract string TableName { get; }
    protected abstract string ArchiveTableName { get; }
    protected abstract string PartitionFunction { get; }
    protected abstract string PartitionScheme { get; }
    protected abstract string[] ExportColumns { get; }
    protected abstract ArchiveTableConfig CategoryConfig { get; }
    protected abstract ArchiveTableConfig? EntityConfig { get; }

    protected int KeepDaysInRealtime => EntityConfig?.KeepDaysInRealtime ?? CategoryConfig.KeepDaysInRealtime!.Value;
    protected int ArchiveRetentionMonths => EntityConfig?.ArchiveRetentionMonths ?? CategoryConfig.ArchiveRetentionMonths!.Value;
    protected int ExportHoldbackDays => EntityConfig?.ExportHoldbackDays ?? CategoryConfig.ExportHoldbackDays!.Value;
    protected int MaxPartitionsPerRun => EntityConfig?.MaxPartitionsPerRun ?? CategoryConfig.MaxPartitionsPerRun!.Value;
    protected bool ExportToWarehouse => EntityConfig?.ExportToWarehouse ?? CategoryConfig.ExportToWarehouse!.Value;
    protected bool DeleteStorageOnPrune => EntityConfig?.DeleteStorageOnPrune ?? CategoryConfig.DeleteStorageOnPrune!.Value;
}
```

Concrete job per table:

```csharp
public class Archive{Entity}Job(
    ServicesWrapper servicesWrapper,
    IRepositoryWrapper repositoryWrapper,
    PartitionArchiveSettings settings,
    IStorageService storageService)
    : PartitionArchiveJobBase<{Entity}, {Entity}Archive>(
        servicesWrapper, repositoryWrapper, settings, storageService)
{
    protected override string TableName => "dbo.{Entities}";
    protected override string ArchiveTableName => "dbo.{Entities}_Archive";
    protected override string PartitionFunction => "PF_{Entities}_CreatedDay";
    protected override string PartitionScheme => "PS_{Entities}_CreatedDay";
    protected override string[] ExportColumns => ["Id", ...];
    protected override ArchiveTableConfig CategoryConfig => settings.{Category};
    protected override ArchiveTableConfig? EntityConfig => settings.{Entity};
}
```

#### Job 1: SlideToArchiveAsync()

Moves closed partitions from active → archive via partition switching.

```
[DisableConcurrentExecution(18000)]  // 5-hour lock
[Queue("archive")]
```

**Implementation:**
1. Set `ArchiveInfo.SetPartitionArchivingStarted()`
2. Ensure future daily boundaries exist via `ExtendBoundariesAsync()`
3. Calculate cutoff from `tableSettings.KeepDaysInRealtime`, using IANA timezone via `TimeZoneInfo.FindSystemTimeZoneById()`
4. Query `sys.partitions` + `sys.partition_range_values` to find partitions where `range_end <= cutoffUtc` with rows
5. Within a **SQL transaction**, for each partition:
   ```sql
   ALTER TABLE {TableName} SWITCH PARTITION {n} TO {ArchiveTableName} PARTITION {n}
   ```
6. Increment `ArchiveInfo.RowsArchived`
7. On ANY error → full transaction rollback
8. Call `SetArchiveFinishedAsync(ArchiveStep.PartitionSwitch, startTime, error)`

**Key SQL — get partitions:**
```sql
WITH rv AS (
  SELECT ROW_NUMBER() OVER (ORDER BY CONVERT(datetime2(7), rv.value)) AS boundary_id,
         CONVERT(datetime2(7), rv.value) AS boundary_value
  FROM sys.partition_range_values rv
  JOIN sys.partition_functions pf ON pf.function_id = rv.function_id
  WHERE pf.name = @partitionFunction
),
parts AS (
  SELECT p.partition_number,
         ISNULL(re.boundary_value, CONVERT(datetime2(7), '9999-12-31')) AS range_end,
         p.rows
  FROM sys.partitions p
  LEFT JOIN rv re ON re.boundary_id = p.partition_number
  WHERE p.object_id = OBJECT_ID(@table) AND p.index_id IN (0,1)
)
SELECT partition_number, rows
FROM parts
WHERE range_end <= @cutoff AND rows > 0
ORDER BY partition_number;
```

#### Job 2: ExportToStorageAsync()

Exports archive partitions to cloud storage as Parquet files.

**Implementation:**
1. Query exportable partitions — partitions in archive table with rows, where `range_end` is before today minus `ExportHoldbackDays`, and NOT already in shared `ExportLedger` table (filtered by `TableName`)
2. Limit to `MaxPartitionsPerRun` partitions per execution
3. For each partition:
   a. Count rows: `SELECT COUNT_BIG(*) FROM {ArchiveTableName} WITH (NOLOCK) WHERE Created >= @from AND Created < @to`
   b. Stream results to temp Parquet file (columnar, compressed)
   c. Upload via `IStorageService` to date-partitioned path: `archive/{entity}/{yyyy}/{MM}/{dd}/{entity}_{yyyyMMdd}.parquet`
   d. Insert into shared `ExportLedger` with `TableName`, `DayStartUtc`, `DayEndUtc`, `RowCount`
   e. Delete temp file
4. Call `SetArchiveFinishedAsync(ArchiveStep.ExportToStorage, startTime, error)`

#### Job 3: DropOldArchivePartitionsAsync()

Enforces retention policy — truncates old archive partitions, cleans up empty boundaries, optionally deletes old Parquet files.

**Implementation:**
1. Calculate cutoff: `today.AddMonths(-ArchiveRetentionMonths)` converted to UTC
2. Find partitions where `range_end <= cutoffUtc` with rows
3. Within a **transaction**, for each partition:
   ```sql
   TRUNCATE TABLE {ArchiveTableName} WITH (PARTITIONS ({n}))
   ```
4. Prune ExportLedger rows: `WHERE TableName == TableName && DayEndUtc <= cutoffUtc`
5. Delete Parquet files from storage (if `DeleteStorageOnPrune = true`)
6. Call `MergeEmptyLeftBoundariesAsync()` — sliding window cleanup
7. Call `SetArchiveFinishedAsync(ArchiveStep.PruneArchive, startTime, error)`

#### Job 4: ExtendBoundariesAsync()

Ensures forward partition boundaries exist for future dates.

**Implementation:**
1. Build date range: today → today + `ForwardBoundaryBufferDays`
2. Fetch existing boundaries from partition function
3. For each missing daily boundary:
   a. Convert local date to UTC using `TimeZoneInfo.FindSystemTimeZoneById()` with IANA timezone ID
   b. Pre-provision filegroup: `ALTER PARTITION SCHEME {PartitionScheme} NEXT USED [PRIMARY]`
   c. Split: `ALTER PARTITION FUNCTION {PartitionFunction}() SPLIT RANGE (@boundaryUtc)`
   d. Skip duplicates gracefully (catch SqlException 7707)

### Shared Utilities

#### ParquetExportHelper
- Uses `Parquet.Net` or `Apache.Arrow` for .NET
- Maps entity columns to Parquet schema
- Compressed (Snappy or Gzip)
- Streaming write — no full dataset in memory

#### IndexAlignmentHelper
- Ensures all indexes on a table are aligned to the partition scheme
- Create index ON partition scheme if missing
- Rebuild with `DROP_EXISTING=ON` if off-scheme

### Settings

#### ArchiveTableConfig record

```csharp
public record ArchiveTableConfig
{
    public int? KeepDaysInRealtime { get; set; }
    public int? ArchiveRetentionMonths { get; set; }
    public int? ExportHoldbackDays { get; set; }
    public int? MaxPartitionsPerRun { get; set; }
    public bool? ExportToWarehouse { get; set; }
    public bool? DeleteStorageOnPrune { get; set; }
}
```

#### PartitionArchiveSettings class

```csharp
public class PartitionArchiveSettings(ConfigurationWrapper c, IWebHostEnvironment e)
    : AppSettingsBase("PartitionArchiveSettings", c, e)
{
    // Global
    public int CommandTimeoutSeconds => GetSanitizedValue(nameof(CommandTimeoutSeconds), 30, 86400, 7200);
    public int ForwardBoundaryBufferDays => GetSanitizedValue(nameof(ForwardBoundaryBufferDays), 30, 180, 90);
    public string StorageContainerPath => GetConfiguration(nameof(StorageContainerPath), "archive");
    public string WarehouseConnectionString => GetConfiguration(nameof(WarehouseConnectionString), "");

    // Category defaults — JSON with all properties populated
    public ArchiveTableConfig Transactional => GetJsonConfiguration<ArchiveTableConfig>(
        nameof(Transactional),
        """{"KeepDaysInRealtime":30,"ArchiveRetentionMonths":12,"ExportHoldbackDays":0,"MaxPartitionsPerRun":60,"ExportToWarehouse":true,"DeleteStorageOnPrune":false}""");
    public ArchiveTableConfig HighVolume => GetJsonConfiguration<ArchiveTableConfig>(...);
    public ArchiveTableConfig ShortLived => GetJsonConfiguration<ArchiveTableConfig>(...);
    public ArchiveTableConfig Listing => GetJsonConfiguration<ArchiveTableConfig>(...);

    // Per-entity overrides — empty string default
    public ArchiveTableConfig? {Entity} => GetJsonConfiguration<ArchiveTableConfig>(nameof({Entity}), "");
}
```

#### Category classification

| Category | KeepDays | Retention | Description |
|----------|----------|-----------|-------------|
| Transactional | 30 | 12mo | Entities tied to financial operations |
| HighVolume | 7 | 12mo | Very high-write, low-value-per-row entities |
| ShortLived | 7 | 6mo | Ephemeral entities with short useful life |
| Listing | 365 | 12mo | User-created content with natural expiry, including children |

Resolution: entity override → category default.

### Timezone Handling

Use IANA timezone IDs with `TimeZoneInfo.FindSystemTimeZoneById()` — works cross-platform on .NET 6+:

```csharp
private static readonly TimeZoneInfo Tz =
    TimeZoneInfo.FindSystemTimeZoneById("{iana-timezone-id}");
```

MUST NOT use `TimeZoneInfo.CreateCustomTimeZone()` — IANA IDs are standard and self-documenting.

---