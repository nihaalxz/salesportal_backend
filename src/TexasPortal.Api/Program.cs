using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TexasPortal.Infrastructure;
using TexasPortal.Api.Extensions;

// Configure QuestPDF License
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);


// Add infrastructure services (EF Core, ISalesReadService, ICurrentUser, etc.)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRealtimeInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// ProblemDetails for structured error responses (RFC 9457)
builder.Services.AddProblemDetails();

// Swagger/OpenAPI — include bearer auth scheme for manual API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Texas Sales Portal API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS for SvelteKit Dev Server
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.WithOrigins(
                  "https://accountstexas.runasp.net",
                  "https://operationstexas.runasp.net",
                  "https://operationtexas.runasp.net",
                  "https://salestexas.runasp.net",
                  "http://localhost:5173", 
                  "http://localhost:4173", 
                  "http://localhost:8001", 
                  "http://localhost:8002", 
                  "http://localhost:8003", 
                  "http://localhost:5174", 
                  "http://localhost:5175",
                  "http://127.0.0.1:5173", 
                  "http://127.0.0.1:4173", 
                  "http://127.0.0.1:8001", 
                  "http://127.0.0.1:8002", 
                  "http://127.0.0.1:8003")
              .SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Bearer & DevMock authentication setup
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured. Add it to appsettings.json or User Secrets.");

var authBuilder = builder.Services.AddAuthentication(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DefaultAuthenticateScheme = "DevOrJwtScheme";
        options.DefaultChallengeScheme = "DevOrJwtScheme";
    }
    else
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }
});

if (builder.Environment.IsDevelopment())
{
    authBuilder.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TexasPortal.Api.Controllers.Sales.DevMockAuthHandler>("DevMockScheme", null);
    
    authBuilder.AddPolicyScheme("DevOrJwtScheme", "Dev or JWT Bearer Scheme Selector", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                if (token == "dev-token-salesman-a" || token == "dev-token-salesman-b" || token == "dev-token-no-permission")
                {
                    return "DevMockScheme";
                }
            }
            return JwtBearerDefaults.AuthenticationScheme;
        };
    });
}

var salesJwtKey = builder.Configuration["Jwt:Key"] ?? "TexasPortalSalesPortalDevSecretKey2024!MustBe64CharsForHS512Security!!";
var accountsJwtKey = builder.Configuration["AccountsPortal:JwtKey"] ?? "tx_ops_super_secret_key_999_must_be_long_enough";

var validationKeys = new SecurityKey[]
{
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(salesJwtKey)),
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accountsJwtKey))
};

authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false; // set to true in production behind HTTPS
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = false,
        ValidateAudience         = false,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeys        = validationKeys,
        ClockSkew                = TimeSpan.FromMinutes(5)
    };

    options.Events ??= new JwtBearerEvents();
    options.Events.ConfigureJwtBearerQueryStringForSignalR();
});

authBuilder.AddScheme<TexasPortal.Api.Authentication.InternalServiceSchemeOptions, TexasPortal.Api.Authentication.InternalServiceAuthenticationHandler>("InternalServiceScheme", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperationsServiceAccess", policy =>
        policy.AddAuthenticationSchemes("InternalServiceScheme")
              .RequireAuthenticatedUser()
              .RequireRole("ServicePrincipal"));
});

// Register Lead Reminder background worker service
builder.Services.AddHostedService<TexasPortal.Api.Services.LeadReminderBackgroundService>();

// Initialize SMTP Settings in NotificationHelper
TexasPortal.Application.Sales.Notifications.NotificationHelper.SmtpServer = builder.Configuration["Smtp:Server"] ?? "sales.texastravels.bh";
TexasPortal.Application.Sales.Notifications.NotificationHelper.SmtpPort = int.TryParse(builder.Configuration["Smtp:Port"], out var port) ? port : 465;
TexasPortal.Application.Sales.Notifications.NotificationHelper.SenderEmail = builder.Configuration["Smtp:SenderEmail"] ?? "info@sales.texastravels.bh";
TexasPortal.Application.Sales.Notifications.NotificationHelper.SenderPassword = builder.Configuration["Smtp:SenderPassword"] ?? "MqFZP2&@Y!yd4rF8";
TexasPortal.Application.Sales.Notifications.NotificationHelper.EnableSsl = !bool.TryParse(builder.Configuration["Smtp:EnableSsl"], out var ssl) || ssl;
TexasPortal.Application.Sales.Notifications.NotificationHelper.AccountsPortalBaseUrl = builder.Configuration["AccountsPortal:BaseUrl"] ?? "https://accountstexas.runasp.net";
TexasPortal.Application.Sales.Notifications.NotificationHelper.AccountsPortalApiKey = builder.Configuration["AccountsPortal:ApiKey"] ?? "tx_secure_service_key_999_32chars_long";

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TexasPortal.Infrastructure.Persistence.SalesPortalDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying EF Core migrations.");
    }
}

app.UseCors("DevCorsPolicy");

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    }
});

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRealtimeHubs();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
