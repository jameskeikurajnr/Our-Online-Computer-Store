using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;

namespace OnlineComputerStore.Tests.TestHelpers
{
    // Every service test that touches the database gets its own private, in-memory
    // SQLite database — same provider the real app uses, so behavior (like the
    // manually-assigned 5-digit Order.Id working with a store-generated key column)
    // matches production rather than diverging the way EF Core's InMemory provider can.
    public abstract class DatabaseTestBase : IDisposable
    {
        private readonly SqliteConnection _connection;
        protected readonly StoreDbContext Context;

        protected DatabaseTestBase()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<StoreDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new StoreDbContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
