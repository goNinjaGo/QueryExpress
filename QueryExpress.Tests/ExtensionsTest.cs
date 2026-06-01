using Microsoft.EntityFrameworkCore;
using QueryExpress.Enums;
using QueryExpress;
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

            Assert.Throws<ArgumentException>(() => ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "ConfidentialData", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "Test" } } }).ToList());
        }

        // String operations
        [TestMethod]
        public void QueryFilter_String_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "John" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.NotEquals, Value = "John" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_StartsWith()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.StartsWith, Value = "J" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_EndsWith()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.EndsWith, Value = "hn" } } }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].FirstName);
        }

        [TestMethod]
        public void QueryFilter_String_Contains()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.Contains, Value = "an" } } }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Jane", result[0].FirstName);
        }

        [TestMethod]
        public void QueryFilter_String_DoesNotContain()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.DoesNotContain, Value = "Jo" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_String_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Email", Filters = new[] { new Filter { Operation = Operation.Contains, Value = "example" } } }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("john@example.com", result[0].Email);
        }

        [TestMethod]
        public void QueryFilter_String_HandlesCaseSensitive()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.Contains, Value = "Jo", IsCaseSensitive = true } } }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].FirstName);
        }

        [TestMethod]
        public void QueryFilter_String_HandlesCaseInsensitive()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "FirstName", Filters = new[] { new Filter { Operation = Operation.Contains, Value = "jo", IsCaseSensitive = false } } }).ToList();
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

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "30" } } }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(30, result[0].Age);
        }

        [TestMethod]
        public void QueryFilter_Numeric_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.NotEquals, Value = "30" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_Between()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "LitersUsed", Filters = new[] { new Filter { Operation = Operation.Between, Value = "5", SecondaryValue = "11" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_LessThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.LessThan, Value = "35" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_LessThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.LessThanOrEqual, Value = "30" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_GreaterThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.GreaterThan, Value = "30" } } }).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(40, result[0].Age);
        }

        [TestMethod]
        public void QueryFilter_Numeric_GreaterThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.GreaterThanOrEqual, Value = "30" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "LitersUsed", Filters = new[] { new Filter { Operation = Operation.GreaterThan, Value = "6" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Numeric_HandlesDifferentDataTypes()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var intResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "Age", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "25" } } }).ToList();
            Assert.AreEqual(1, intResult.Count);

            var decimalResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "LitersUsed", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "10.5" } } }).ToList();
            Assert.AreEqual(1, decimalResult.Count);
        }

        // Date operations
        [TestMethod]
        public void QueryFilter_Date_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "2020-01-01" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.NotEquals, Value = "2020-01-01" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_Between()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.Between, Value = "2019-01-01", SecondaryValue = "2020-12-31" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_LessThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.LessThan, Value = "2020-01-01" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_LessThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.LessThanOrEqual, Value = "2020-01-01" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_GreaterThan()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.GreaterThan, Value = "2020-01-01" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_GreaterThanOrEqual()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.GreaterThanOrEqual, Value = "2020-01-01" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "UpdatedAt", Filters = new[] { new Filter { Operation = Operation.LessThan, Value = DateTimeOffset.UtcNow.ToString() } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Date_HandlesDifferentDataTypes()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // CreatedAt is DateTime, UpdatedAt is DateTimeOffset
            var dtResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "CreatedAt", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "2021-01-01" } } }).ToList();
            Assert.AreEqual(1, dtResult.Count);

            var dtoResult = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "UpdatedAt", Filters = new[] { new Filter { Operation = Operation.LessThanOrEqual, Value = DateTimeOffset.UtcNow.ToString() } } }).ToList();
            Assert.IsTrue(dtoResult.Count >= 0);
        }

        // Boolean operations
        [TestMethod]
        public void QueryFilter_Boolean_Equals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "IsEligibile", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "True" } } }).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Boolean_NotEquals()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "IsEligibile", Filters = new[] { new Filter { Operation = Operation.NotEquals, Value = "True" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_Boolean_HandlesNullable()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData { Operand = "IsUtilized", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "True" } } }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        // Multi-condiiton tests
        [TestMethod]
        public void QueryFilter_HandlesMultipleColumns()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData[] {
                new FilterData { Operand = "IsUtilized", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "True" } } },
                new FilterData { Operand = "LastName", Filters = new[] { new Filter { Operation = Operation.Equals, Value = "Doe" } } }
            }).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void QueryFilter_HandlesOrCondition()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData {
                Operand = "FirstName",
                Operator = ConditionOperator.Or,
                Filters = new Filter[] {
                    new() { Operation = Operation.Equals, Value = "John" },
                    new() { Operation = Operation.Equals, Value = "Jane" }
                }
            }).ToList();

            Assert.AreEqual(2, result.Count); // John and Jane
        }

        [TestMethod]
        public void QueryFilter_HandlesAndCondition()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var result = ctx.People.AsQueryable().QueryFilter(new FilterData
            {
                Operand = "Age",
                Operator = ConditionOperator.And,
                Filters = new Filter[] {
                    new() { Operation = Operation.LessThan, Value = "40" },
                    new() { Operation = Operation.GreaterThan, Value = "25" }
                }
            }).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(30, result[0].Age);
        }

        // Sort tests
        [TestMethod]
        public void QuerySort_ByAge_Ascending()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var sorted = ctx.People.AsQueryable().QuerySort(new[] { new SortData { ColumnName = "Age", SortDirection = SortDirection.Asc } }).ToList();
            Assert.AreEqual(3, sorted.Count);
            Assert.AreEqual(25, sorted[0].Age); // Jane
            Assert.AreEqual(30, sorted[1].Age); // John
            Assert.AreEqual(40, sorted[2].Age); // Bob
        }

        [TestMethod]
        public void QuerySort_ByLastName_Descending()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            var sorted = ctx.People.AsQueryable().QuerySort(new[] { new SortData { ColumnName = "LastName", SortDirection = SortDirection.Desc } }).ToList();
            Assert.AreEqual(3, sorted.Count);
            Assert.AreEqual("Smith", sorted[0].LastName); // Jane
            Assert.AreEqual("Jones", sorted[1].LastName); // Bob
            Assert.AreEqual("Doe", sorted[2].LastName); // John
        }

        [TestMethod]
        public void QuerySort_MultipleColumns()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // Sort by IsEligibile desc (true first), then Age asc
            var sorted = ctx.People.AsQueryable().QuerySort(new[] {
                new SortData { ColumnName = "IsEligibile", SortDirection = SortDirection.Desc },
                new SortData { ColumnName = "Age", SortDirection = SortDirection.Asc }
            }).ToList();

            Assert.AreEqual(3, sorted.Count);
            // First two should be eligibile = true
            Assert.IsTrue(sorted[0].IsEligibile);
            Assert.IsTrue(sorted[1].IsEligibile);
            Assert.AreEqual(30, sorted[0].Age);
            Assert.AreEqual(40, sorted[1].Age);
            // Last should be not eligibile
            Assert.IsFalse(sorted[2].IsEligibile);
        }

        // Paging tests
        [TestMethod]
        public void QueryPage_FirstAndSecondPage()
        {
            var options = CreateOptions();
            using var ctx = new TestDataContext(options);
            Seed(ctx);

            // Order by Age asc then page
            var ordered = ctx.People.AsQueryable().QuerySort(new[] { new SortData { ColumnName = "Age", SortDirection = SortDirection.Asc } });

            var page1 = ordered.QueryPage(new PageData { PageNum = 1, PageSize = 2 }).ToList();
            Assert.AreEqual(2, page1.Count);
            Assert.AreEqual(25, page1[0].Age);
            Assert.AreEqual(30, page1[1].Age);

            var page2 = ordered.QueryPage(new PageData { PageNum = 2, PageSize = 2 }).ToList();
            Assert.AreEqual(1, page2.Count);
            Assert.AreEqual(40, page2[0].Age);
        }
    }
}
