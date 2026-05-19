using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueryExpress.Web.Api.Models;
using Microsoft.EntityFrameworkCore;
using QueryExpress;
using QueryExpress.Tests.Data.Entity;
using System.Linq;


namespace QueryExpress.Web.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<Person> QueryPeople([FromBody] DataQuery? dataQuery)
        {
            // Use an in-memory database for demo/testing purposes
            var options = new DbContextOptionsBuilder<TestDataContext>()
                .UseInMemoryDatabase("PeopleDb")
                .Options;

            using var ctx = new TestDataContext(options);

            // Seed sample data if none exists
            if (!ctx.People.Any())
            {
                ctx.People.AddRange(new List<Tests.Data.Models.Person>
                {
                    new() { FirstName = "John", LastName = "Doe", Email = "john@example.com", Age = 30, LitersUsed = 10.5m, CreatedAt = new DateTime(2020,1,1), UpdatedAt = DateTimeOffset.UtcNow, IsEligibile = true, IsUtilized = true, ConfidentialData = "secret1" },
                    new() { FirstName = "Jane", LastName = "Smith", Email = "jane@sample.com", Age = 25, LitersUsed = null, CreatedAt = new DateTime(2021,1,1), UpdatedAt = null, IsEligibile = false, IsUtilized = null, ConfidentialData = "secret2" },
                    new() { FirstName = "Bob", LastName = "Jones", Email = null, Age = 40, LitersUsed = 5m, CreatedAt = new DateTime(2019,12,31), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1), IsEligibile = true, IsUtilized = false, ConfidentialData = "secret3" }
                });
                ctx.SaveChanges();
            }

            IQueryable<Tests.Data.Models.Person> query = ctx.People.AsQueryable();

            if(dataQuery != null)
            {
                query = query
                    .QueryFilter(dataQuery.FilterData)
                    .QuerySort(dataQuery.SortData)
                    .QueryPage(dataQuery.PageData);
            }

            var results = query
                .Select(p => new Person
                {
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    Age = p.Age,
                    LitersUsed = p.LitersUsed,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsEligibile = p.IsEligibile,
                    IsUtilized = p.IsUtilized
                })
                .ToList();

            return results;
        }
    }
}
