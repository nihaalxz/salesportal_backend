using System.Linq;
using TexasPortal.Domain.Common;

namespace TexasPortal.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ScopeToUser<T>(this IQueryable<T> query, long userId) where T : class, IUserOwned
    {
        return query.Where(x => x.UserId == userId);
    }

    public static IQueryable<T> ScopeToCreator<T>(this IQueryable<T> query, long userId) where T : class, ICreatorOwned
    {
        return query.Where(x => x.CreatedBy == userId);
    }
}
