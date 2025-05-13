using EzyTaskin.Data;
using EzyTaskin.Services;
using EzyTaskin.Subscriptions;
using EzyTaskin.Utils;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Background;

class PremiumLifetimeService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PremiumLifetimeService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(PremiumLifetimeService)} is starting.");
        stoppingToken.Register(() =>
        {
            logger.LogInformation($"{nameof(PremiumLifetimeService)} is stopping.");
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            await DeactivateExpiredPremium();

            try
            {
                var now = DateTime.UtcNow;
                var tomorrow = now.Date.AddDays(1);
                await Task.Delay(tomorrow - now, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(
                    $"{nameof(PremiumLifetimeService)} Task.Delay failed with: {{Exception}}", e
                );
            }
        }
    }

    private async Task DeactivateExpiredPremium()
    {
        try
        {
            logger.LogInformation($"{nameof(DeactivateExpiredPremium)} check is running.");

            using var scope = serviceScopeFactory.CreateScope();

            var notificationService = scope.ServiceProvider
                .GetRequiredService<NotificationService>();
            var paymentService  = scope.ServiceProvider
                .GetRequiredService<PaymentService>();

            var dbContextOptions =
                scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
            var dbContext = new ApplicationDbContext(dbContextOptions);
            var transaction = await dbContext.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow.Date;
            var lastMonth = now.AddMonths(-1);

            await dbContext.Providers
                .Where(p => p.IsSubscriptionActive == false)
                .Where(p => p.SubscriptionDate != null)
                .Where(p => p.SubscriptionDate < lastMonth)
                .ExecuteUpdateAsync(p =>
                    p.SetProperty(p => p.IsPremium, false)
                        .SetProperty(p => p.IsSubscriptionActive, false)
                        .SetProperty(p => p.SubscriptionDate, (DateTime?)null)
                );

            var dbProviders = await dbContext.Providers
                .Include(p => p.Account)
                .Where(p => p.IsSubscriptionActive)
                .Where(p => p.SubscriptionDate != null)
                .Where(p => p.SubscriptionDate < lastMonth)
                .ToListAsync();

            foreach (var dbProvider in dbProviders)
            {
                // Reset the subscription state, in case anything fails.
                dbProvider.IsPremium = false;
                dbProvider.IsSubscriptionActive = false;
                dbProvider.SubscriptionDate = null;

                try
                {
                    var paymentMethod =
                        await paymentService
                            .GetPaymentMethods(Guid.Parse(dbProvider.Account.Id))
                            .FirstOrDefaultAsync();

                    if (paymentMethod is not null)
                    {
                        await paymentService.Debit(
                            paymentMethod.Id,
                            Premium.Instance.CalculatePrice(dbProvider.ToModel())
                        );
                        dbProvider.IsPremium = true;
                        dbProvider.IsSubscriptionActive = true;
                        dbProvider.SubscriptionDate = now;

                        await notificationService.SendNotification(new()
                        {
                            Timestamp = now,
                            Account = Guid.Parse(dbProvider.Account.Id),
                            Title = "Subscription",
                            Content = "Your premium subscription has been successfully extended."
                        });
                    }
                }
                catch (Exception e)
                {
                    logger.LogInformation(
                        $"{nameof(DeactivateExpiredPremium)} failed: {{Exception}}", e
                    );
                }

                if (dbProvider.IsPremium == false)
                {
                    await notificationService.SendNotification(new()
                    {
                        Timestamp = now,
                        Account = Guid.Parse(dbProvider.Account.Id),
                        Title = "Subscription",
                        Content = "Your premium subscription has been canceled, since " +
                            "you do not have a valid payment method."
                    });
                }

                dbContext.Providers.Update(dbProvider);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation($"{nameof(DeactivateExpiredPremium)} check completed.");
        }
        catch (Exception e)
        {
            logger.LogError($"{nameof(DeactivateExpiredPremium)} failed with: {{Exception}}", e);
        }
    }
}
