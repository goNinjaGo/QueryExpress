using QueryExpress.Attributes;

namespace QueryExpress.Tests.Data.Metadata
{
    public class PersonMetadata
    {
        [NonSearchable]
        public string? ConfidentialData { get; set; }
    }
}
