using QueryExpress.Enums;

namespace QueryExpress
{
    public class FilterData
    {
        public string Operand { get; set; } = string.Empty;
        public ConditionOperator Operator { get; set; } = ConditionOperator.And;
        public IEnumerable<Filter> Filters { get; set; } = [];
    }
}
