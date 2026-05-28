namespace QueryExpress.Web.Api.Models
{
    public class ApiResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = [];
        public int TotalRecords { get; set; }
    }
}
