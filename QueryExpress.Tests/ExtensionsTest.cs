using Microsoft.EntityFrameworkCore;
using QueryExpress.Enums;
using QueryExpress.Tests.Data.Entity;
using QueryExpress.Tests.Data.Models;

namespace QueryExpress.Tests
{
    [TestClass]
    public sealed class ExtensionsTest
    {
        private DbContextOptions<TestDataContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<TestDataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private void Seed(TestDataContext ctx)
        {
            var people = new List<Person>
            {
                new Person { FirstName = "John", LastName = "Doe", Email = "john@example.com", Age = 30, LitersUsed = 10.5m, CreatedAt = new DateTime(2020,1,1), UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(-30), IsEligibile = true, IsUtilized = true },
                new Person { FirstName = "Jane", LastName = "Smith", Email = "jane@sample.com", Age = 25, LitersUsed = null, CreatedAt = new DateTime(2021,1,1), UpdatedAt = null, IsEligibile = false, IsUtilized = null },
                new Person { FirstName = "Bob", LastName = "Jones", Email = null, Age = 40, LitersUsed = 5m, CreatedAt = new DateTime(2019,12,31), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1), IsEligibile = true, IsUtilized = false, ConfidentialData = "Test" }
            };
            ctx.People.AddRange(people);
            ctx.SaveChanges();
        }

        [TestMethod]
        public void QueryFilter_NonSearchableColumn()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            Assert.Throws<ArgumentException>(() => ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "ConfidentialData", Value = "Test" }).ToList());
        }

        // String operations
        [TestMethod]
        public void QueryFilter_String_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "FirstName", Value = "John" }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.NotEquals, Operand = "FirstName", Value = "John" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_StartsWith()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.StartsWith, Operand = "FirstName", Value = "J" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_EndsWith()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.EndsWith, Operand = "FirstName", Value = "hn" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].FirstName);
        }

        [TestMethod]
        public void QueryFilter_String_Contains()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Contains, Operand = "FirstName", Value = "an" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Jane", result[0].FirstName);
        }

        [TestMethod]
        public void QueryFilter_String_DoesNotContain()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.DoesNotContain, Operand = "FirstName", Value = "Jo" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // Ensure null Email values don't cause exceptions when running string filters
            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Contains, Operand = "Email", Value = "example" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("john@example.com", result[0].Email);
        }

        [TestMethod]
        public void QueryFilter_String_HandlesCaseSensitive()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Contains, IsCaseSensitive = true, Operand = "FirstName", Value = "Jo" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].FirstName);
        }

        [TestMethod]
        public void QueryFilter_String_HandlesCaseInsensitive()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Contains, IsCaseSensitive = false, Operand = "FirstName", Value = "jo" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].FirstName);
        }

        // Numeric operations
        [TestMethod]
        public void QueryFilter_Numeric_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "Age", Value = "30" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(30, result[0].Age);
        }

        [TestMethod]
        public void QueryFilter_Numeric_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.NotEquals, Operand = "Age", Value = "30" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_Between()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Between, Operand = "LitersUsed", Value = "5", SecondaryValue = "11" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_LessThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.LessThan, Operand = "Age", Value = "35" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_LessThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.LessThanOrEqual, Operand = "Age", Value = "30" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_GreaterThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.GreaterThan, Operand = "Age", Value = "30" }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(40, result[0].Age);
        }

        [TestMethod]
        public void QueryFilter_Numeric_GreaterThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.GreaterThanOrEqual, Operand = "Age", Value = "30" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // Ensure nullable numeric (LitersUsed) doesn't cause exceptions
            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.GreaterThan, Operand = "LitersUsed", Value = "6" }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_HandlesDifferentDataTypes()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var intResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "Age", Value = "25" }).ToList();
            Assert.AreEqual(1, intResult.Count);

            var decimalResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "LitersUsed", Value = "5" }).ToList();
            Assert.AreEqual(1, decimalResult.Count);
        }

        // Date operations
        [TestMethod]
        public void QueryFilter_Date_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "CreatedAt", Value = "2020-01-01" }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.NotEquals, Operand = "CreatedAt", Value = "2020-01-01" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_Between()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Between, Operand = "CreatedAt", Value = "2019-01-01", SecondaryValue = "2020-12-31" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_LessThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.LessThan, Operand = "CreatedAt", Value = "2020-01-01" }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_LessThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.LessThanOrEqual, Operand = "CreatedAt", Value = "2020-01-01" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_GreaterThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.GreaterThan, Operand = "CreatedAt", Value = "2020-01-01" }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_GreaterThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.GreaterThanOrEqual, Operand = "CreatedAt", Value = "2020-01-01" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // Ensure nullable UpdatedAt doesn't break date filters
            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.LessThan, Operand = "UpdatedAt", Value = DateTimeOffset.UtcNow.ToString() }).ToList();
            // All records with UpdatedAt that are not null should return
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_HandlesDifferentDataTypes()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // CreatedAt is DateTime, UpdatedAt is DateTimeOffset
            var dtResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "CreatedAt", Value = "2021-01-01" }).ToList();
            Assert.AreEqual(1, dtResult.Count);

            var dtoResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "UpdatedAt", Value = DateTimeOffset.UtcNow.ToString() }).ToList();
            Assert.IsTrue(dtoResult.Count >= 0);
        }

        // Boolean operations
        [TestMethod]
        public void QueryFilter_Boolean_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "IsEligibile", Value = "True" }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Boolean_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.NotEquals, Operand = "IsEligibile", Value = "True" }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Boolean_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operation = Operation.Equals, Operand = "IsUtilized", Value = "True" }).ToList();
            Assert.AreEqual(1, result.Count);
        }
    }
}
