using QueryExpress.Attributes;
using QueryExpress.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryExpress
{
    public static class Extensions
    {
        public static IQueryable<T> QuerySort<T>(this IQueryable<T> query, IEnumerable<SortData> sortExpressions)
        {
            ParameterExpression param = Expression.Parameter(typeof(T));
            for (int i = 0; i < sortExpressions.Count(); i++)
            {
                var index = i;
                MemberExpression prop = Expression.PropertyOrField(param, sortExpressions.ElementAt(index).ColumnName);
                LambdaExpression sort = Expression.Lambda(prop, param);

                var functionName = index == 0 ? "OrderBy" : "ThenBy";
                functionName = sortExpressions.ElementAt(index).SortDirection == SortDirection.Desc ? functionName + "Descending" : functionName;

                MethodCallExpression resultExp = Expression.Call(typeof(Queryable), functionName, [typeof(T), prop.Type], query.Expression, Expression.Quote(sort));

                query = query.Provider.CreateQuery<T>(resultExp);
            }

            return query;
        }

        public static IQueryable<T> QueryPage<T>(this IQueryable<T> query, PageData pageData)
        {
            return query.Skip((pageData.PageNum - 1) * pageData.PageSize).Take(pageData.PageSize);
        }

        public static IQueryable<T> QueryFilter<T>(this IQueryable<T> query, IEnumerable<FilterData> filterData)
        {
            foreach (var filter in filterData)
            {
                query = query.QueryFilter(filter);
            }
            return query;

        }

        public static IQueryable<T> QueryFilter<T>(this IQueryable<T> query, FilterData filterData)
        {
            if (string.IsNullOrEmpty(filterData.Value)) { return query; }
            var type = typeof(T);
            ParameterExpression param = Expression.Parameter(type);
            MemberExpression prop = Expression.PropertyOrField(param, filterData.Operand);

            var metadataType = type.GetCustomAttributes(typeof(MetadataTypeAttribute), true)
                .OfType<MetadataTypeAttribute>().FirstOrDefault();

            if (prop.Member.GetCustomAttribute<NonSearchableAttribute>(inherit: true) != null 
                || metadataType?.MetadataClassType.GetProperty(filterData.Operand)?.GetCustomAttribute<NonSearchableAttribute>(inherit: true) != null)
            {
                throw new ArgumentException($"{filterData.Operand} is not searchable");
            }

            var propType = prop.Type;
            propType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (propType == typeof(string))
            {
                if (Constants.StringOperations.Contains(filterData.Operation))
                {
                    return StringFilter(query, filterData, param, prop);
                }
                else
                {
                    throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for string type.");
                }
            }
            else if (Constants.DateTypes.Contains(propType))
            {
                if (Constants.DateOperations.Contains(filterData.Operation))
                {
                    return DateFilter(query, filterData, param, prop, propType);
                }
                else
                {
                    throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for date type.");
                }
            }
            else if (Constants.NumericTypes.Contains(propType))
            {
                if (Constants.NumericOperations.Contains(filterData.Operation))
                {
                    return NumericFilter(query, filterData, param, prop, propType);
                }
                else
                {
                    throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for numeric type.");
                }
            }
            else if (propType == typeof(bool))
            {
                if (Constants.BooleanOperations.Contains(filterData.Operation))
                {
                    return BooleanFilter(query, filterData, param, prop);
                }
                else
                {
                    throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for boolean type.");
                }
            }
            else
            {
                throw new NotSupportedException($"Type {propType} is not supported.");
            }
        }

        private static IQueryable<T> StringFilter<T>(this IQueryable<T> query, FilterData filterData, ParameterExpression param, MemberExpression prop)
        {
            string methodName = filterData.Operation switch
            {
                Operation.DoesNotContain => "Contains",
                Operation.NotEquals => "Equals",
                _ => filterData.Operation.ToString()
            };
            var method = filterData.IsCaseSensitive ? typeof(string).GetMethod(methodName, [typeof(string)]) 
                : typeof(string).GetMethod(methodName, [typeof(string), typeof(StringComparison)]);
            if (method != null)
            {
               var args = filterData.IsCaseSensitive ? new Expression[] { Expression.Constant(filterData.Value, typeof(string)) }
                    : [Expression.Constant(filterData.Value, typeof(string)), Expression.Constant(StringComparison.InvariantCultureIgnoreCase, typeof(StringComparison))];

                var call = Expression.Call(prop, method, args);
                var lambda = Expression.Lambda<Func<T, bool>>(call, param);

                if (filterData.Operation == Operation.DoesNotContain || filterData.Operation == Operation.NotEquals)
                {
                    var notCall = Expression.Not(call);
                    lambda = Expression.Lambda<Func<T, bool>>(notCall, param);
                }

                return query.Where(lambda);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static IQueryable<T> DateFilter<T>(this IQueryable<T> query, FilterData filterData, ParameterExpression param, MemberExpression prop, Type propType)
        {
            var converter = TypeDescriptor.GetConverter(propType);
            var value = converter.ConvertFromString(filterData.Value) ?? throw new ArgumentException("Value is not a valid date");
            var valueExpression = Expression.Constant(value, prop.Type);

            if (filterData.Operation == Operation.Between)
            {
                var secondaryValue = converter.ConvertFromString(filterData.SecondaryValue ?? throw new ArgumentException("Secondary value is required for Between operation.")) 
                    ?? throw new ArgumentException($"Secondary value {filterData.SecondaryValue} is not a valid date.");
                if ((DateTime)secondaryValue < (DateTime)value)
                {
                    throw new ArgumentException($"Second value must be greater than or equal to first value for Between operation.");
                }

                var secondaryValueExpression = Expression.Constant(secondaryValue, prop.Type);
                var greaterThanOrEqual = Expression.GreaterThanOrEqual(prop, valueExpression);
                var lessThanOrEqual = Expression.LessThanOrEqual(prop, secondaryValueExpression);
                var betweenExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
                var betweenLambda = Expression.Lambda<Func<T, bool>>(betweenExpression, param);
                return query.Where(betweenLambda);
            }

            BinaryExpression comparison = filterData.Operation switch
            {
                Operation.Equals => Expression.Equal(prop, valueExpression),
                Operation.NotEquals => Expression.NotEqual(prop, valueExpression),
                Operation.LessThan => Expression.LessThan(prop, valueExpression),
                Operation.LessThanOrEqual => Expression.LessThanOrEqual(prop, valueExpression),
                Operation.GreaterThan => Expression.GreaterThan(prop, valueExpression),
                Operation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(prop, valueExpression),
                _ => throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for date type.")
            };
            var lambda = Expression.Lambda<Func<T, bool>>(comparison, param);
            return query.Where(lambda);            
        }

        private static IQueryable<T> NumericFilter<T>(this IQueryable<T> query, FilterData filterData, ParameterExpression param, MemberExpression prop, Type propType)
        {
            var converter = TypeDescriptor.GetConverter(propType);
            var value = converter.ConvertFromString(filterData.Value) ?? throw new ArgumentException("Value is not a valid number"); ;
            var valueExpression = Expression.Constant(value, prop.Type);
            if (filterData.Operation == Operation.Between)
            {
                var secondaryValue = converter.ConvertFromString(filterData.SecondaryValue ?? throw new ArgumentException("Secondary value is required for Between operation."))
                    ?? throw new ArgumentException("Secondary value is not a valid number");
                if (double.Parse(filterData.Value) > double.Parse(filterData.SecondaryValue))
                {
                    throw new ArgumentException("Second value must be greater than or equal to first value for Between operation.");
                }
                var secondaryValueExpression = Expression.Constant(secondaryValue, prop.Type);
                var greaterThanOrEqual = Expression.GreaterThanOrEqual(prop, valueExpression);
                var lessThanOrEqual = Expression.LessThanOrEqual(prop, secondaryValueExpression);
                var betweenExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
                var betweenLambda = Expression.Lambda<Func<T, bool>>(betweenExpression, param);
                return query.Where(betweenLambda);                
            }
            BinaryExpression comparison = filterData.Operation switch
            {
                Operation.Equals => Expression.Equal(prop, valueExpression),
                Operation.NotEquals => Expression.NotEqual(prop, valueExpression),
                Operation.LessThan => Expression.LessThan(prop, valueExpression),
                Operation.LessThanOrEqual => Expression.LessThanOrEqual(prop, valueExpression),
                Operation.GreaterThan => Expression.GreaterThan(prop, valueExpression),
                Operation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(prop, valueExpression),
                _ => throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for numeric type.")
            };
            var lambda = Expression.Lambda<Func<T, bool>>(comparison, param);
            return query.Where(lambda);
        }

        private static IQueryable<T> BooleanFilter<T>(this IQueryable<T> query, FilterData filterData, ParameterExpression param, MemberExpression prop)
        {
            if (bool.TryParse(filterData.Value, out bool value))
            {
                var valueExpression = Expression.Constant(value, prop.Type);
                BinaryExpression comparison = filterData.Operation switch
                {
                    Operation.Equals => Expression.Equal(prop, valueExpression),
                    Operation.NotEquals => Expression.NotEqual(prop, valueExpression),
                    _ => throw new InvalidOperationException($"Operation {filterData.Operation} is not valid for boolean type.")
                };
                var lambda = Expression.Lambda<Func<T, bool>>(comparison, param);
                return query.Where(lambda);
            }
            else
            {
                throw new FormatException($"Value {filterData.Value} is not a valid boolean.");
            }
        }
    }
}
