using QueryExpress.Tests.Data.Metadata;
using System.ComponentModel.DataAnnotations;

namespace QueryExpress.Tests.Data.Models
{
    [MetadataType(typeof(PersonMetadata))]
    public partial class Person
    {
        public static List<Person> GetTestPeople()
        {
            return new List<Person>
            {
                new Person { FirstName = "John", LastName = "Doe", Email = "john@example.com", Age = 30, LitersUsed = 10.5m, CreatedAt = new DateTime(2020,1,1), UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(-30), IsEligibile = true, IsUtilized = true },
                new Person { FirstName = "Jane", LastName = "Smith", Email = "jane@sample.com", Age = 25, LitersUsed = null, CreatedAt = new DateTime(2021,1,1), UpdatedAt = null, IsEligibile = false, IsUtilized = null },
                new Person { FirstName = "Bob", LastName = "Jones", Email = null, Age = 40, LitersUsed = 5m, CreatedAt = new DateTime(2019,12,31), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1), IsEligibile = true, IsUtilized = false, ConfidentialData = "Test" }
            };
        }
    }
}
