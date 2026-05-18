using Microsoft.EntityFrameworkCore;
using QueryExpress.Tests.Data.Models;

namespace QueryExpress.Tests.Data.Entity;

public class TestDataContext : DbContext
{
    public TestDataContext(DbContextOptions<TestDataContext> options) : base(options)
    {
    }

    public DbSet<Person> People { get; set; } = null!;
}
