# Source Profile Tooling Plan

This document details the plan to extract database schema mappings and metadata from the source Laravel MySQL database safely.

---

## 1. Context & Connection Constraints

- **Source Provider**: MySQL / MariaDB (legacy Laravel environment).
- **Current Block**: Direct connections to the local port `3307` or remote credentials are blocked in the developer host environment.
- **Solution**: Execute a local Artisan CLI command inside the legacy Laravel directory to profile the schema and dump metadata to a structured JSON file.

---

## 2. Artisan Profiler Command Design

We implement a Laravel Artisan Command `php artisan portal:profile-schema` that scans tables, queries `INFORMATION_SCHEMA`, and exports table attributes to `docs/migration/source-schema-profile.json`.

```php
<?php

namespace App\Console\Commands;

use Illuminate\Console\Command;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

class ProfileSchema extends Command
{
    protected $signature = 'portal:profile-schema';
    protected $description = 'Profile schema information and dump to JSON for .NET migration verification';

    public function handle()
    {
        $tables = DB::select('SHOW TABLES');
        $dbName = DB::connection()->getDatabaseName();
        $keyName = "Tables_in_" . $dbName;
        
        $profile = [];
        foreach ($tables as $table) {
            $tableName = $table->$keyName;
            $columns = Schema::getColumnListing($tableName);
            
            $tableMeta = [];
            foreach ($columns as $column) {
                $type = Schema::getColumnType($tableName, $column);
                $tableMeta[$column] = [
                    'type' => $type,
                    'nullable' => !Schema::getConnection()->getDoctrineColumn($tableName, $column)->getNotnull()
                ];
            }
            $profile[$tableName] = $tableMeta;
        }
        
        file_put_contents(base_path('docs/migration/source-schema-profile.json'), json_encode($profile, JSON_PRETTY_PRINT));
        $this->info('Schema profile successfully exported.');
    }
}
```

---

## 3. Usage inside the Verification Pipeline

Once `source-schema-profile.json` is generated, our .NET verification tests read the file and compare it directly to SQL Server `INFORMATION_SCHEMA` metadata, raising automated alerts if any column type or nullability mismatch is detected.
