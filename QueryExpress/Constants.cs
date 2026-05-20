using QueryExpress.Enums;

namespace QueryExpress
{
    public static class Constants
    {
        public static int DefaultPageSize = 10;

        public static string NullValue = "null";

        public static SortDirection DefaultSortDirection = SortDirection.Asc;

        public static Type[] NumericTypes = new Type[] {
            typeof(sbyte), 
            typeof(short), 
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(nint),
            typeof(nuint),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        public static Type[] DateTypes = new Type[] {
            typeof(DateTime),
            typeof(DateTimeOffset)
        };

        public static Operation[] StringOperations = new Operation[] {
            Operation.Equals,
            Operation.NotEquals,
            Operation.Contains,
            Operation.DoesNotContain,
            Operation.StartsWith,
            Operation.EndsWith,
        };

        public static Operation[] NumericOperations = new Operation[] {
            Operation.Equals,
            Operation.NotEquals,
            Operation.Between,
            Operation.LessThan,
            Operation.LessThanOrEqual,
            Operation.GreaterThan,
            Operation.GreaterThanOrEqual
        };

        public static Operation[] DateOperations = new Operation[] {
            Operation.Equals,
            Operation.NotEquals,
            Operation.Between,
            Operation.LessThan,
            Operation.LessThanOrEqual,
            Operation.GreaterThan,
            Operation.GreaterThanOrEqual
        };

        public static Operation[] BooleanOperations = new Operation[] {
            Operation.Equals,
            Operation.NotEquals
        };
    }
}
