using QueryExpress.Enums;
using System.Linq.Expressions;

namespace QueryExpress
{
    public static class Extensions
    {
        public static IQueryable<T> QuerySort<T>(this IQueryable<T> query, IEnumerable<(string column, SortDirection direction)> sortExpressions)
        {
            ParameterExpression param = Expression.Parameter(typeof(T));
            for (int i = 0; i < sortExpressions.Count(); i++)
            {
                var index = i;
                MemberExpression prop = Expression.PropertyOrField(param, sortExpressions.ElementAt(index).column);
                LambdaExpression sort = Expression.Lambda(prop, param);

                var functionName = index == 0 ? "OrderBy" : "ThenBy";
                functionName = sortExpressions.ElementAt(index).direction == SortDirection.Desc ? functionName + "Descending" : functionName;

                MethodCallExpression resultExp = Expression.Call(typeof(Queryable), functionName, [typeof(T), prop.Type], query.Expression, Expression.Quote(sort));

                query = query.Provider.CreateQuery<T>(resultExp);
            }

            return query;
        }

        public static IQueryable<T> QueryPage<T>(this IQueryable<T> query, PageData pageData)
        {
            return query.Skip((pageData.PageNum - 1) * pageData.PageSize).Take(pageData.PageSize);
        }

        public static IQueryable<T> QueryFilter<T>(this IQueryable<T> query, FilterData filterData)
        {
            throw new NotImplementedException();
        }
    }
}
