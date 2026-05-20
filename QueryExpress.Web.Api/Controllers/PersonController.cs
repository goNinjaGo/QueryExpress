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
        private readonly TestDataContext testDataContext;
        public PersonController(TestDataContext testDataContext) 
        { 
            this.testDataContext = testDataContext;
        }

        [HttpGet]
        [HttpPost]
        public IEnumerable<Person> QueryPeople([FromBody] DataQuery? dataQuery)
        {
            IQueryable<Tests.Data.Models.Person> query = testDataContext.People.AsQueryable();

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
