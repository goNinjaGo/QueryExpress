using QueryExpress.Tests.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueryExpress.Tests.Data
{
    public static class SeedData
    {
        public static List<Person> People = new List<Person>
        {
            new() { FirstName = "John", LastName = "Doe", Email = "john@example.com", Age = 30, LitersUsed = 10.5m, CreatedAt = new DateTime(2020,1,1), UpdatedAt = DateTimeOffset.UtcNow, IsEligibile = true, IsUtilized = true, ConfidentialData = "secret1" },
            new() { FirstName = "Jane", LastName = "Smith", Email = "jane@sample.com", Age = 25, LitersUsed = null, CreatedAt = new DateTime(2021,1,1), UpdatedAt = null, IsEligibile = false, IsUtilized = null, ConfidentialData = "secret2" },
            new() { FirstName = "Bob", LastName = "Jones", Email = null, Age = 40, LitersUsed = 5m, CreatedAt = new DateTime(2019,12,31), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1), IsEligibile = true, IsUtilized = false, ConfidentialData = "secret3" }
        };

    }
}
