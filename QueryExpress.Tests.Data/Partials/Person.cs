using QueryExpress.Tests.Data.Metadata;
using System.ComponentModel.DataAnnotations;

namespace QueryExpress.Tests.Data.Models
{
    [MetadataType(typeof(PersonMetadata))]
    public partial class Person
    {
    }
}
