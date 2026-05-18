using QueryExpress.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueryExpress
{
    public class SortData
    {
        public string ColumnName { get; set; } = string.Empty;
        public SortDirection SortDirection { get; set; } = Constants.DefaultSortDirection;
    }
}
