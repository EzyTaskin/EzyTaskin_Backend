using EzyTaskin.Data.Db;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<Account>(options)
{
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Message>(b =>
        {
            b.HasOne(e => e.Sender).WithMany();
            b.HasOne(e => e.Receiver).WithMany();
        });
    }
}
