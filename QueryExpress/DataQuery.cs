namespace QueryExpress
{
    public class DataQuery
    {
        public SortData[] SortData { get; set; } = new SortData[0];
        public PageData PageData { get; set; } = new PageData { PageNum = 1, PageSize = Constants.DefaultPageSize };
        public FilterData[] FilterData { get; set; } = new FilterData[0];
    }
}
