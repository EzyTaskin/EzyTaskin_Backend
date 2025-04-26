using EzyTaskin.Data;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Services;

public abstract class DbService(DbContextOptions<ApplicationDbContext> dbContextOptions)
{
    protected ApplicationDbContext DbContext => new(dbContextOptions);
}
