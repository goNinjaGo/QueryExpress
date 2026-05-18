using Microsoft.EntityFrameworkCore;
using QueryExpress.Tests.Data.Entity;

namespace QueryExpress.Tests.Data;

public static class TestDataContextFactory
{
    public static TestDataContext CreateInMemoryContext(string dbName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<TestDataContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var ctx = new TestDataContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
