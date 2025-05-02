using EzyTaskin.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Utils;

public static class QueryExtensions
{
    public static IQueryable<Request> Saturate(this IQueryable<Request> query)
        => query
            .Include(r => r.Consumer)
                .ThenInclude(c => c.Account)
            .Include(r => r.Selected)
                .ThenInclude(o => o!.Provider)
                    .ThenInclude(p => p.Account);

    public static IQueryable<RequestCategory> Saturate(this IQueryable<RequestCategory> query)
        => query
            .Include(rc => rc.Category)
            .Include(rc => rc.Request)
                .ThenInclude(r => r.Consumer)
                    .ThenInclude(c => c.Account)
            .Include(rc => rc.Request)
                .ThenInclude(r => r.Selected)
                    .ThenInclude(o => o!.Provider)
                        .ThenInclude(p => p.Account);

    public static IQueryable<Offer> Saturate(this IQueryable<Offer> query)
        => query
            .Include(r => r.Provider)
                .ThenInclude(p => p.Account)
            .Include(r => r.Request);
}
