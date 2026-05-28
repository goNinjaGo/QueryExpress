using LinqKit;
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
            var filters = filterData.Filters?.ToArray() ?? [];
            if (filters.Length == 0)
            {
                return query;
            }

            var type = typeof(T);
            var param = Expression.Parameter(type);
            var prop = Expression.PropertyOrField(param, filterData.Operand);

            ValidateSearchable(type, prop, filterData.Operand);

            var propType = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
            var predicate = PredicateBuilder.New<T>(filterData.Operator == ConditionOperator.And);
            var hasPredicate = false;

            foreach (var filter in filters)
            {
                if (IsEmptyFilter(filter))
                {
                    continue;
                }

                var filterPredicate = BuildFilterExpression<T>(filter, param, prop, propType);
                predicate = filterData.Operator == ConditionOperator.Or
                    ? predicate.Or(filterPredicate)
                    : predicate.And(filterPredicate);
                hasPredicate = true;
            }

            return hasPredicate ? query.Where(predicate) : query;
        }

        private static void ValidateSearchable(Type type, MemberExpression prop, string operand)
        {
            var metadataType = type.GetCustomAttributes(typeof(MetadataTypeAttribute), true)
                .OfType<MetadataTypeAttribute>().FirstOrDefault();

            if (prop.Member.GetCustomAttribute<NonSearchableAttribute>(inherit: true) != null
                || metadataType?.MetadataClassType.GetProperty(operand)?.GetCustomAttribute<NonSearchableAttribute>(inherit: true) != null)
            {
                throw new ArgumentException($"{operand} is not searchable");
            }
        }

        private static bool IsEmptyFilter(Filter filter)
        {
            return string.IsNullOrEmpty(filter.Value)
                || filter.Value == Constants.NullValue
                || (filter.Operation == Operation.Between
                    && (string.IsNullOrEmpty(filter.SecondaryValue) || filter.SecondaryValue == Constants.NullValue));
        }

        private static Expression<Func<T, bool>> BuildFilterExpression<T>(Filter filter, ParameterExpression param, MemberExpression prop, Type propType)
        {
            if (propType == typeof(string))
            {
                if (!Constants.StringOperations.Contains(filter.Operation))
                {
                    throw new InvalidOperationException($"Operation {filter.Operation} is not valid for string type.");
                }

                return StringFilter<T>(filter, param, prop);
            }

            if (Constants.DateTypes.Contains(propType))
            {
                if (!Constants.DateOperations.Contains(filter.Operation))
                {
                    throw new InvalidOperationException($"Operation {filter.Operation} is not valid for date type.");
                }

                return DateFilter<T>(filter, param, prop, propType);
            }

            if (Constants.NumericTypes.Contains(propType))
            {
                if (!Constants.NumericOperations.Contains(filter.Operation))
                {
                    throw new InvalidOperationException($"Operation {filter.Operation} is not valid for numeric type.");
                }

                return NumericFilter<T>(filter, param, prop, propType);
            }

            if (propType == typeof(bool))
            {
                if (!Constants.BooleanOperations.Contains(filter.Operation))
                {
                    throw new InvalidOperationException($"Operation {filter.Operation} is not valid for boolean type.");
                }

                return BooleanFilter<T>(filter, param, prop);
            }

            throw new NotSupportedException($"Type {propType} is not supported.");
        }

        private static Expression<Func<T, bool>> StringFilter<T>(Filter filter, ParameterExpression param, MemberExpression prop)
        {
            string methodName = filter.Operation switch
            {
                Operation.DoesNotContain => "Contains",
                Operation.NotEquals => "Equals",
                _ => filter.Operation.ToString()
            };

            var method = filter.IsCaseSensitive
                ? typeof(string).GetMethod(methodName, [typeof(string)])
                : typeof(string).GetMethod(methodName, [typeof(string), typeof(StringComparison)]);

            if (method == null)
            {
                throw new InvalidOperationException();
            }

            var args = filter.IsCaseSensitive
                ? new Expression[] { Expression.Constant(filter.Value, typeof(string)) }
                : [Expression.Constant(filter.Value, typeof(string)), Expression.Constant(StringComparison.InvariantCultureIgnoreCase, typeof(StringComparison))];

            var call = Expression.Call(prop, method, args);
            Expression expression = filter.Operation == Operation.DoesNotContain || filter.Operation == Operation.NotEquals
                ? Expression.Not(call)
                : call;

            return Expression.Lambda<Func<T, bool>>(expression, param);
        }

        private static Expression<Func<T, bool>> DateFilter<T>(Filter filter, ParameterExpression param, MemberExpression prop, Type propType)
        {
            var converter = TypeDescriptor.GetConverter(propType);
            var value = converter.ConvertFromString(filter.Value) ?? throw new ArgumentException("Value is not a valid date");
            var valueExpression = CreateValueExpression(value, prop.Type, propType);

            if (filter.Operation == Operation.Between)
            {
                var secondaryValue = converter.ConvertFromString(filter.SecondaryValue!) ?? throw new ArgumentException("SecondaryValue is not a valid date");
                if (((IComparable)secondaryValue).CompareTo(value) < 0)
                {
                    return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), param);
                }

                var secondaryValueExpression = CreateValueExpression(secondaryValue, prop.Type, propType);
                var greaterThanOrEqual = Expression.GreaterThanOrEqual(prop, valueExpression);
                var lessThanOrEqual = Expression.LessThanOrEqual(prop, secondaryValueExpression);
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual), param);
            }

            var comparison = filter.Operation switch
            {
                Operation.Equals => Expression.Equal(prop, valueExpression),
                Operation.NotEquals => Expression.NotEqual(prop, valueExpression),
                Operation.LessThan => Expression.LessThan(prop, valueExpression),
                Operation.LessThanOrEqual => Expression.LessThanOrEqual(prop, valueExpression),
                Operation.GreaterThan => Expression.GreaterThan(prop, valueExpression),
                Operation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(prop, valueExpression),
                _ => throw new InvalidOperationException($"Operation {filter.Operation} is not valid for date type.")
            };

            return Expression.Lambda<Func<T, bool>>(comparison, param);
        }

        private static Expression<Func<T, bool>> NumericFilter<T>(Filter filter, ParameterExpression param, MemberExpression prop, Type propType)
        {
            var converter = TypeDescriptor.GetConverter(propType);
            var value = converter.ConvertFromString(filter.Value) ?? throw new ArgumentException("Value is not a valid number");
            var valueExpression = CreateValueExpression(value, prop.Type, propType);

            if (filter.Operation == Operation.Between)
            {
                var secondaryValue = converter.ConvertFromString(filter.SecondaryValue!) ?? throw new ArgumentException("SecondaryValue is not a valid number");
                if (((IComparable)secondaryValue).CompareTo(value) < 0)
                {
                    return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), param);
                }

                var secondaryValueExpression = CreateValueExpression(secondaryValue, prop.Type, propType);
                var greaterThanOrEqual = Expression.GreaterThanOrEqual(prop, valueExpression);
                var lessThanOrEqual = Expression.LessThanOrEqual(prop, secondaryValueExpression);
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual), param);
            }

            var comparison = filter.Operation switch
            {
                Operation.Equals => Expression.Equal(prop, valueExpression),
                Operation.NotEquals => Expression.NotEqual(prop, valueExpression),
                Operation.LessThan => Expression.LessThan(prop, valueExpression),
                Operation.LessThanOrEqual => Expression.LessThanOrEqual(prop, valueExpression),
                Operation.GreaterThan => Expression.GreaterThan(prop, valueExpression),
                Operation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(prop, valueExpression),
                _ => throw new InvalidOperationException($"Operation {filter.Operation} is not valid for numeric type.")
            };

            return Expression.Lambda<Func<T, bool>>(comparison, param);
        }

        private static Expression<Func<T, bool>> BooleanFilter<T>(Filter filter, ParameterExpression param, MemberExpression prop)
        {
            if (!bool.TryParse(filter.Value, out bool value))
            {
                return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), param);
            }

            var valueExpression = CreateValueExpression(value, prop.Type, typeof(bool));
            var comparison = filter.Operation switch
            {
                Operation.Equals => Expression.Equal(prop, valueExpression),
                Operation.NotEquals => Expression.NotEqual(prop, valueExpression),
                _ => throw new InvalidOperationException($"Operation {filter.Operation} is not valid for boolean type.")
            };

            return Expression.Lambda<Func<T, bool>>(comparison, param);
        }

        private static Expression CreateValueExpression(object value, Type propType, Type valueType)
        {
            var valueExpression = Expression.Constant(value, valueType);
            return propType == valueType ? valueExpression : Expression.Convert(valueExpression, propType);
        }
    }
}
