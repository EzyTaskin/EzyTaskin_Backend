using EzyTaskin.Data.Db;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<Account>(options)
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Consumer> Consumers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Offer> Offers { get; set; }
    public DbSet<ProviderCategory> ProviderCategories { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<RequestCategory> RequestCategories { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(b =>
        {
            // No-op.
        });

        builder.Entity<Consumer>(b =>
        {
            b.HasOne(e => e.Account).WithOne()
                .HasForeignKey<Consumer>($"{nameof(Account)}Id");
        });

        builder.Entity<Message>(b =>
        {
            b.HasOne(e => e.Sender).WithMany();
            b.HasOne(e => e.Receiver).WithMany();
        });

        builder.Entity<Notification>(b =>
        {
            b.HasOne(e => e.Account).WithMany();
        });

        builder.Entity<PaymentMethod>(b =>
        {
            b.HasOne(e => e.Account).WithMany();
            b.HasDiscriminator(e => e.Type)
                .HasValue<CardPaymentMethod>(nameof(CardPaymentMethod));
        });

        builder.Entity<Offer>(b =>
        {
            b.HasOne(e => e.Provider).WithMany();
            b.HasOne(e => e.Request).WithMany(r => r.Offers);
        });

        builder.Entity<ProviderCategory>(b =>
        {
            b.HasOne(e => e.Provider).WithMany();
            b.HasOne(e => e.Category).WithMany();
        });

        builder.Entity<Provider>(b =>
        {
            b.HasOne(e => e.Account).WithOne()
                .HasForeignKey<Provider>($"{nameof(Account)}Id");
        });

        builder.Entity<RequestCategory>(b =>
        {
            b.HasOne(e => e.Request).WithMany();
            b.HasOne(e => e.Category).WithMany();
        });

        builder.Entity<Request>(b =>
        {
            b.HasOne(e => e.Consumer).WithMany();
        });

        builder.Entity<Review>(b =>
        {
            b.HasOne(e => e.Request).WithOne()
                .HasForeignKey<Review>($"{nameof(Request)}Id");
        });
    }
}
