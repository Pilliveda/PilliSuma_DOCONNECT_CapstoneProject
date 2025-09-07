using System;
using System.Threading.Tasks;
using DoConnect.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DoConnect.Tests.Integration
{
    /// <summary>
    /// Creates a dedicated SQL Server test database (LocalDB by default) and drops it after tests.
    /// Set DOCONNECT_TEST_SQL_BASE to override the server/credentials.
    /// </summary>
    public class SqlServerDbFixture : IAsyncDisposable
    {
        private static readonly string BaseConn =
            Environment.GetEnvironmentVariable("DOCONNECT_TEST_SQL_BASE")
            ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True;";

        public string DatabaseName { get; } = "DoConnect_Tests_" + Guid.NewGuid().ToString("N");
        public string ConnectionString => $"{BaseConn}Database={DatabaseName};";

        public AppDbContext CreateContext(bool useMigrations = false)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString);
            var ctx = new AppDbContext(builder.Options);

            if (useMigrations) ctx.Database.Migrate();
            else ctx.Database.EnsureCreated();

            return ctx;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // Drop the database forcefully
                var masterConnStr = BaseConn; // without DB
                await using var conn = new SqlConnection(masterConnStr);
                await conn.OpenAsync();

                var dbName = DatabaseName.Replace("]", "]]");
                var sql = $@"
IF DB_ID('{dbName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{dbName}];
END";
                await using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* swallow cleanup errors */ }
        }
    }

    [CollectionDefinition("SqlServer collection")]
    public class SqlServerCollection : ICollectionFixture<SqlServerDbFixture> { }
}
