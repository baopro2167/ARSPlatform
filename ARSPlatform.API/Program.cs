using ARSPlatform.API.HostedServices;
using ARSPlatform.MODEL;
using ARSPlatform.REPO;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPOSITORIES;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.Interfaces;
using ARSPlatform.SERVICE.Mapping;
using ARSPlatform.SERVICES;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using ARSPlatform.SERVICE;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Register Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
// Automatically register all repositories ending with "Repository" via reflection
var repoAssembly = typeof(UserRepository).Assembly;
foreach (var type in repoAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository") && t != typeof(GenericRepository<>)))
{
    var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name == $"I{type.Name}");
    if (interfaceType != null)
    {
        builder.Services.AddScoped(interfaceType, type);
    }
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Nâng giới hạn Form Upload lên 500MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524_288_000;
    options.ValueLengthLimit = 524_288_000;
});

// Nâng giới hạn Server Kestrel lên 500MB
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524_288_000;
});

// Configure PORT for Render/Container deployment
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// Register External API Service
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();

// OpenAlex work preview cache and endpoint rate limiting
builder.Services.AddMemoryCache();

var openAlexPermitLimit =
    int.TryParse(
        Environment.GetEnvironmentVariable("OPENALEX_WORK_LOOKUP_PERMIT_LIMIT")
        ?? builder.Configuration["OpenAlexSettings:WorkLookupPermitLimit"],
        out var configuredOpenAlexPermitLimit)
        ? configuredOpenAlexPermitLimit
        : 30;

var openAlexWindowSeconds =
    int.TryParse(
        Environment.GetEnvironmentVariable("OPENALEX_WORK_LOOKUP_WINDOW_SECONDS")
        ?? builder.Configuration["OpenAlexSettings:WorkLookupWindowSeconds"],
        out var configuredOpenAlexWindowSeconds)
        ? configuredOpenAlexWindowSeconds
        : 60;

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(
        "OpenAlexWorkLookup",
        httpContext =>
        {
            var actorKey =
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: actorKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, openAlexPermitLimit),
                    Window = TimeSpan.FromSeconds(
                        Math.Max(1, openAlexWindowSeconds)),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] =
                Math.Ceiling(retryAfter.TotalSeconds).ToString();
        }

        var actorIdValue =
            context.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;

        if (!int.TryParse(actorIdValue, out var actorId))
        {
            return;
        }

        var actorName =
            context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value
            ?? context.HttpContext.User.Identity?.Name
            ?? context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value
            ?? $"User {actorId}";

        var workId =
            context.HttpContext.Request.RouteValues
                .TryGetValue("workId", out var routeWorkId)
                ? routeWorkId?.ToString()
                : null;

        var auditLogService =
            context.HttpContext.RequestServices
                .GetService<IAuditLogService>();

        if (auditLogService == null)
        {
            return;
        }

        try
        {
            await auditLogService.CreateAsync(
                new AuditLogCreateRequest
                {
                    AdminId = actorId,
                    AdminName = actorName,
                    Action = "OPENALEX_WORK_LOOKUP",
                    Target = "OpenAlexWork",
                    TargetId = workId,
                    Details = JsonSerializer.Serialize(
                        new
                        {
                            Provider = "OpenAlex",
                            Outcome = "RateLimitRejected",
                            Timestamp = DateTime.UtcNow
                        })
                });
        }
        catch
        {
            // Rate-limit response must not fail because audit persistence failed.
        }
    };
});

// Register OpenAlex Settings and Service
builder.Services.Configure<OpenAlexSettings>(options =>
{
    var section = builder.Configuration.GetSection("OpenAlexSettings");

    options.BaseUrl =
        section["BaseUrl"]
        ?? "https://api.openalex.org";

    options.ApiKey =
        Environment.GetEnvironmentVariable("OPENALEX_API_KEY")
        ?? section["ApiKey"]
        ?? "";

    options.TimeoutSeconds =
        int.TryParse(
            section["TimeoutSeconds"],
            out var timeoutSeconds)
            ? timeoutSeconds
            : 15;

    options.MaxWorks =
        int.TryParse(
            section["MaxWorks"],
            out var maxWorks)
            ? maxWorks
            : 100;

    options.WorkCacheSeconds =
        int.TryParse(
            Environment.GetEnvironmentVariable("OPENALEX_WORK_CACHE_SECONDS")
            ?? section["WorkCacheSeconds"],
            out var workCacheSeconds)
            ? workCacheSeconds
            : 300;

    options.WorkLookupPermitLimit =
        int.TryParse(
            Environment.GetEnvironmentVariable("OPENALEX_WORK_LOOKUP_PERMIT_LIMIT")
            ?? section["WorkLookupPermitLimit"],
            out var workLookupPermitLimit)
            ? workLookupPermitLimit
            : 30;

    options.WorkLookupWindowSeconds =
        int.TryParse(
            Environment.GetEnvironmentVariable("OPENALEX_WORK_LOOKUP_WINDOW_SECONDS")
            ?? section["WorkLookupWindowSeconds"],
            out var workLookupWindowSeconds)
            ? workLookupWindowSeconds
            : 60;
});

builder.Services.AddHttpClient<IOpenAlexService, OpenAlexService>(
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<OpenAlexSettings>>()
            .Value;

        var baseUrl =
            string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://api.openalex.org"
                : settings.BaseUrl.TrimEnd('/');

        client.BaseAddress =
            new Uri($"{baseUrl}/");

        client.Timeout =
            TimeSpan.FromSeconds(
                Math.Clamp(
                    settings.TimeoutSeconds,
                    3,
                    60));

        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/json");

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    settings.ApiKey);
        }
    });

// Register ORCID OAuth Settings
builder.Services.Configure<OrcidSettings>(options =>
{
    var section =
        builder.Configuration.GetSection("OrcidSettings");

    options.AuthorizationUrl =
        Environment.GetEnvironmentVariable(
            "ORCID_AUTHORIZATION_URL")
        ?? section["AuthorizationUrl"]
        ?? "https://orcid.org/oauth/authorize";

    options.TokenUrl =
        Environment.GetEnvironmentVariable(
            "ORCID_TOKEN_URL")
        ?? section["TokenUrl"]
        ?? "https://orcid.org/oauth/token";

    options.ClientId =
        Environment.GetEnvironmentVariable(
            "ORCID_CLIENT_ID")
        ?? section["ClientId"]
        ?? "";

    options.ClientSecret =
        Environment.GetEnvironmentVariable(
            "ORCID_CLIENT_SECRET")
        ?? section["ClientSecret"]
        ?? "";

    options.RedirectUri =
        Environment.GetEnvironmentVariable(
            "ORCID_REDIRECT_URI")
        ?? section["RedirectUri"]
        ?? "";

    options.Scope =
        Environment.GetEnvironmentVariable(
            "ORCID_SCOPE")
        ?? section["Scope"]
        ?? "/authenticate";

    options.TimeoutSeconds =
        int.TryParse(
            Environment.GetEnvironmentVariable(
                "ORCID_TIMEOUT_SECONDS")
            ?? section["TimeoutSeconds"],
            out var orcidTimeoutSeconds)
            ? orcidTimeoutSeconds
            : 15;
});

builder.Services.AddHttpClient(
    "OrcidOAuth",
    (serviceProvider, client) =>
    {
        var settings = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<OrcidSettings>>()
            .Value;

        client.Timeout =
            TimeSpan.FromSeconds(
                Math.Clamp(
                    settings.TimeoutSeconds,
                    3,
                    60));

        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/json");
    });

// Register Audio Summary Service
// Timeout 15 phút để xử lý audio lớn
builder.Services.AddHttpClient<IAudioSummaryService, AudioSummaryService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});

// Register Google Meet Settings and Service
builder.Services.Configure<GoogleMeetSettings>(
    builder.Configuration.GetSection("GoogleMeetSettings"));
builder.Services.AddHttpClient<IGoogleMeetService, GoogleMeetService>();

// Register Services automatically via reflection
var serviceAssembly = typeof(AuthService).Assembly;
foreach (var type in serviceAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service") && !t.Name.EndsWith("HostedService")))
{
    var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name == $"I{type.Name}");
    if (interfaceType != null &&
        !builder.Services.Any(descriptor =>
            descriptor.ServiceType == interfaceType))
    {
        builder.Services.AddScoped(interfaceType, type);
    }
}
builder.Services.AddHostedService<SeminarAutomationHostedService>();
builder.Services.AddHostedService<OtpCleanupHostedService>();

// Register PayOS Settings
builder.Services.Configure<PayOSSettings>(options =>
{
    var section = builder.Configuration.GetSection("PayOSSettings");
    options.ClientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID") ?? section["ClientId"] ?? "";
    options.ApiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY") ?? section["ApiKey"] ?? "";
    options.ChecksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY") ?? section["ChecksumKey"] ?? "";
    options.BaseUrl = section["BaseUrl"] ?? "https://api-merchant.payos.vn";
    options.ReturnUrl = section["ReturnUrl"] ?? "";
    options.CancelUrl = section["CancelUrl"] ?? "";
});
builder.Services.AddHttpClient();

// Register Email Settings and Service
builder.Services.Configure<EmailSettings>(options =>
{
    var section = builder.Configuration.GetSection("EmailSettings");

    var senderEmail = Environment.GetEnvironmentVariable("EmailSender")
                      ?? Environment.GetEnvironmentVariable("EMAIL_SENDER")
                      ?? Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL")
                      ?? Environment.GetEnvironmentVariable("EMAIL_USERNAME")
                      ?? section["SenderEmail"]
                      ?? section["Username"]
                      ?? "academicresearchplatform@gmail.com";

    var rawPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
                      ?? section["Password"]
                      ?? "";

    var cleanPassword = rawPassword.Trim().Trim('"').Replace(" ", "");

    options.Server = Environment.GetEnvironmentVariable("EMAIL_SERVER") ?? section["Server"] ?? "smtp.gmail.com";
    options.Port = int.TryParse(Environment.GetEnvironmentVariable("EMAIL_PORT"), out var p) ? p : (int.TryParse(section["Port"], out var sp) ? sp : 587);
    options.SenderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME") ?? section["SenderName"] ?? "Academic Research Platform";
    options.SenderEmail = senderEmail;
    options.Username = senderEmail;
    options.Password = cleanPassword;
    options.VerificationUrl = Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_URL") ?? section["VerificationUrl"] ?? "https://fe-ars.vercel.app/verify-email";
});

// Register Google Calendar Settings from Configuration / Environment Variables
var googleCalendarSection = builder.Configuration.GetSection("GoogleCalendar");
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? googleCalendarSection["ClientId"] ?? "";
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? googleCalendarSection["ClientSecret"] ?? "";
var serviceAccountEmail = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_EMAIL") ?? googleCalendarSection["ServiceAccountEmail"] ?? "";
var privateKey = Environment.GetEnvironmentVariable("GOOGLE_PRIVATE_KEY") ?? googleCalendarSection["PrivateKey"] ?? "";

builder.Services.Configure<GoogleCalendarSettings>(options =>
{
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
    options.ServiceAccountEmail = serviceAccountEmail;
    options.PrivateKey = privateKey;
});
builder.Services.AddHttpClient<IGoogleCalendarService, GoogleCalendarService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var keyString = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? jwtSettings["Key"]
                ?? "ARSPlatformSuperSecretKeyThatIsAtLeast32BytesLong!";

var key = Encoding.UTF8.GetBytes(keyString);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;

    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = true,
            ValidIssuer =
                jwtSettings["Issuer"] ?? "ARSPlatformIssuer",

            ValidateAudience = true,
            ValidAudience =
                jwtSettings["Audience"] ?? "ARSPlatformAudience",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    var approvedUserPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(
            "Researcher",
            "Admin",
            "Reviewer",
            "Lecturer",
            "Graduate Student",
            "Student",
            "Guest")
        .Build();

    options.DefaultPolicy = approvedUserPolicy;
    options.FallbackPolicy = approvedUserPolicy;

    options.AddPolicy("ForumRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(
            "Guest",
            "Researcher",
            "Admin",
            "Reviewer",
            "Lecturer",
            "Graduate Student",
            "Student");
    });
});

// Configure Swagger with Bearer Authentication support
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "ARSPlatform API",
            Version = "v1"
        });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description =
            "Enter JWT Bearer token **_only_** (without 'Bearer ' prefix)",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(
        securityScheme.Reference.Id,
        securityScheme);

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                securityScheme,
                Array.Empty<string>()
            }
        });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ARSPlatform API v1");
    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection();

// Automatically apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    try
    {
        // Drop the old unique constraint that doesn't allow multiple NULLs
        context.Database.ExecuteSqlRaw(@"
            IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ__User__A6FBF2FBCD0C569B' AND type = 'UQ')
            BEGIN
                ALTER TABLE [dbo].[User] DROP CONSTRAINT [UQ__User__A6FBF2FBCD0C569B];
            END
        ");

        // Recreate it as a filtered index that allows multiple NULLs but enforces uniqueness for non-null GoogleId
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_User_GoogleId' AND object_id = OBJECT_ID('dbo.User'))
            BEGIN
                CREATE UNIQUE NONCLUSTERED INDEX UX_User_GoogleId ON [dbo].[User](GoogleId) WHERE GoogleId IS NOT NULL;
            END
        ");
    }
    catch (System.Exception ex)
    {
        Console.WriteLine($"Error updating GoogleId unique index: {ex.Message}");
    }
}



app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();