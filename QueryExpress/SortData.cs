using QueryExpress.Enums;

namespace QueryExpress
{
    public class SortData
    {
        public string ColumnName { get; set; } = string.Empty;
        public SortDirection SortDirection { get; set; } = Constants.DefaultSortDirection;
    }
}
