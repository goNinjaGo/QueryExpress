namespace QueryExpress
{
    public class DataQuery
    {
        public SortData SortData { get; set; } = new SortData();
        public PageData PageData { get; set; } = new PageData { PageNum = 1, PageSize = Constants.DefaultPageSize };
    }
}
