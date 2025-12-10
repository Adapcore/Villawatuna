# Database Migration Guide for Production

## Problem
Migrations are not applying to the server after release/deployment.

## Solution 1: Automatic Migration on Startup (Recommended) ✅

The application has been configured to automatically apply pending migrations when it starts.

**Location:** `Program.cs` (lines 67-84)

```csharp
// Apply pending migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<HotelContext>();
        // Apply pending migrations
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        // Don't throw - allow app to start even if migration fails
    }
}
```

**How it works:**
- When the application starts, it automatically checks for pending migrations
- If any are found, they are applied automatically
- If migration fails, the error is logged but the app still starts (prevents crashes)

**Benefits:**
- ✅ No manual intervention needed
- ✅ Migrations apply automatically on deployment
- ✅ Works in all environments (dev, staging, production)

**Note:** Make sure the database connection string in `appsettings.json` (or production config) is correct and the database user has permissions to alter tables.

---

## Solution 2: Manual Migration via Command Line

If automatic migration doesn't work or you prefer manual control:

### On Development Machine:
```bash
cd HotelManagement
dotnet ef database update --context HotelContext
```

### On Production Server:
1. **Copy the published application to the server**
2. **Ensure migrations are included in publish output** (they should be by default)
3. **Run the migration command:**
   ```bash
   cd /path/to/published/app
   dotnet ef database update --context HotelContext --connection "YourProductionConnectionString"
   ```

**Or use the EF Core tools directly:**
```bash
dotnet ef database update --context HotelContext --project HotelManagement --startup-project HotelManagement
```

---

## Solution 3: Ensure Migrations are Included in Publish

### Check .csproj File
The project file should include migrations. They are automatically included, but verify:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

### Verify Migrations are Published
After publishing, check that the `Migrations` folder exists in the published output:
```
PublishedApp/
  ├── Migrations/
  │   ├── 20251210050130_InvoiceDetail_Quantity_Decimal.cs
  │   ├── 20251210050130_InvoiceDetail_Quantity_Decimal.Designer.cs
  │   └── ... (other migrations)
  └── ...
```

---

## Troubleshooting

### Issue: Migrations not applying automatically

**Check:**
1. **Database Connection String** - Verify it's correct in production `appsettings.json` or environment variables
2. **Database Permissions** - Ensure the database user has `ALTER TABLE` permissions
3. **Application Logs** - Check logs for migration errors:
   ```bash
   # Check application logs for migration errors
   tail -f /var/log/your-app/app.log
   ```
4. **Migration Files** - Verify migrations are included in the published output

### Issue: "Unable to load one or more of the requested types"

This is a known issue with Microsoft.CodeAnalysis package version conflicts. The migration files were created manually to work around this.

**Solution:** The migration files are already created and should work. If you need to create new migrations:
1. Try updating the CodeAnalysis packages to matching versions
2. Or create migrations manually (as was done for the Quantity decimal migration)

### Issue: Migration fails with permission errors

**Solution:** Grant necessary permissions to the database user:
```sql
-- Grant permissions (run as database admin)
GRANT ALTER ON SCHEMA::dbo TO [YourDatabaseUser];
GRANT CREATE TABLE TO [YourDatabaseUser];
```

### Issue: Migration applies but changes don't appear

**Check:**
1. Verify the migration actually ran: Check `__EFMigrationsHistory` table
2. Verify the connection string points to the correct database
3. Check if there are multiple databases/environments

---

## Best Practices

1. **Test Migrations Locally First**
   - Always test migrations on a local copy of production data before deploying

2. **Backup Before Migration**
   - Always backup the database before applying migrations in production
   ```sql
   BACKUP DATABASE HotelManagement TO DISK = 'C:\Backups\HotelManagement_PreMigration.bak'
   ```

3. **Monitor Application Logs**
   - Check logs after deployment to ensure migrations applied successfully

4. **Use Environment-Specific Connection Strings**
   - Use different connection strings for dev/staging/production
   - Store production connection strings securely (Azure Key Vault, environment variables, etc.)

5. **Consider Migration Scripts for Complex Changes**
   - For complex migrations (data migration, large table changes), consider creating SQL scripts
   - Run them manually or via deployment pipeline

---

## Verification

After deployment, verify migrations were applied:

### Check Migration History Table:
```sql
SELECT * FROM __EFMigrationsHistory 
ORDER BY MigrationId DESC;
```

### Verify Schema Changes:
```sql
-- Check if Quantity column is decimal
SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InvoiceDetails' 
  AND COLUMN_NAME = 'Quantity';
```

Expected result:
- `DATA_TYPE`: `decimal`
- `NUMERIC_PRECISION`: `18`
- `NUMERIC_SCALE`: `2`

---

## Deployment Checklist

- [ ] Migrations tested locally
- [ ] Database backup created
- [ ] Connection string verified for production
- [ ] Application published with migrations included
- [ ] Application deployed to server
- [ ] Application logs checked for migration errors
- [ ] Migration history table verified
- [ ] Schema changes verified
- [ ] Application functionality tested

---

## Additional Resources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Applying Migrations in Production](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

