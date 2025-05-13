using EzyTaskin.Background;
using EzyTaskin.Data;
using EzyTaskin.Identity;
using EzyTaskin.Messages.Email;
using EzyTaskin.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PostmarkDotNet;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true);

// Add services to the container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddCors(options =>
{
    var servers = builder.Configuration.GetSection("AllowedOrigins").Get<List<string>>();

    options.AddDefaultPolicy(policy =>
    {
        if (servers is not null && servers.Count > 0)
        {
            policy.WithOrigins([.. servers])
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowedToAllowWildcardSubdomains();
        }
    });
});

var adminDomain = builder.Configuration["Admin:Domain"];
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EzyTaskinEmployee", policy =>
    {
        policy.RequireAssertion(context =>
            context.User.FindFirst(ClaimTypes.Email)?.Value?.EndsWith($"@{adminDomain}")
                ?? false
        );
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google OAuth ClientId not found.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google OAuth ClientSecret not found.");
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]
            ?? throw new InvalidOperationException("Microsoft OAuth ClientId not found.");
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]
            ?? throw new InvalidOperationException("Microsoft OAuth ClientSecret not found.");
    })
    .AddIdentityCookies(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.ApplicationCookie?.Configure(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
            });

            options.ExternalCookie?.Configure(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
            });
        }
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<Account>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddSingleton<IEmailSender<Account>, IdentityEmailSender>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<EmailServiceFactory, NoOpEmailServiceFactory>();
}
else
{
    builder.Services.AddSingleton<EmailServiceFactory>((serviceProvider) =>
        new PostmarkEmailServiceFactory(
            configuration: serviceProvider.GetRequiredService<IConfiguration>(),
            webHostEnvironment: serviceProvider.GetRequiredService<IWebHostEnvironment>(),
            client: new PostmarkClient(builder.Configuration["ApiKeys:Postmark"]
                ?? throw new InvalidOperationException("Postmark API key not found."))
        )
    );
}

builder.Services.AddControllers();

builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<ReviewService>();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

builder.Services.AddHostedService<PremiumLifetimeService>();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    app.UseSwagger(options =>
    {
        var servers = builder.Configuration.GetSection("Swagger:Servers").Get<List<string>>();

        if (servers is not null && servers.Count > 0)
        {
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                swaggerDoc.Servers = [.. servers.Select(s => new OpenApiServer { Url = s })];
            });
        }
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "v1");
    });
}
else
{
    app.UseForwardedHeaders();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString().TrimEnd('/');
    var isApi = path.StartsWith("/api", StringComparison.InvariantCultureIgnoreCase);

    if (!isApi && !path.EndsWith(".html", StringComparison.InvariantCultureIgnoreCase))
    {
        var pathHtml = $"{path}.html";
        var fileInfo = builder.Environment.WebRootFileProvider.GetFileInfo(pathHtml);
        if (fileInfo.Exists)
        {
            context.Request.Path = pathHtml;
        }
    }
    else if (path == string.Empty)
    {
        context.Request.Path = "/index.html";
    }

    if (!isApi)
    {
        context.Response.OnStarting(async () =>
        {
            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                context.Request.Path = "/404.html";
                await next(context);
            }
        });
    }

    await next(context);
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
