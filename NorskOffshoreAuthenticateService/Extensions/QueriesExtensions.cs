using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NorskOffshoreAuthenticateService.Models;

namespace NorskOffshoreAuthenticateService.Extensions;
public static class QueriesExtensions
{
    public static async Task<IEnumerable<UserItem>> WhereAsync(this DbSet<UserItem> dbSet, Expression<Func<UserItem, bool>> predicate)
    {
        return await dbSet.Where(predicate).ToArrayAsync();
    }
}
