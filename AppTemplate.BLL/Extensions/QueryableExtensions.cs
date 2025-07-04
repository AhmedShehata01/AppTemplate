using System.Linq.Expressions;
using System.Reflection;

namespace AppTemplate.BLL.Extensions
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> ApplySorting<T>(this IQueryable<T> source, string? sortBy, string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return source.OrderBy(x => 0); // ترتيب افتراضي بدون تأثير

            var property = typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return source.OrderBy(x => 0); // لو خاصية مش موجودة نرتب عشوائي (مفيش خطأ)

            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);

            string methodName = sortDirection?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";

            var resultExp = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.PropertyType },
                source.Expression,
                Expression.Quote(orderByExp));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExp);
        }
    }
}
