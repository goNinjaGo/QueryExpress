using QueryExpress.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueryExpress
{
    public class Filter
    {
        public Operation Operation { get; set; } = Operation.Equals;
        public string Value { get; set; } = string.Empty;
        public string? SecondaryValue { get; set; }
        public bool IsCaseSensitive { get; set; } = false;
    }
}
