using ARSPlatform.MODEL;
using ARSPlatform.REPO;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPOSITORIES;
using ARSPlatform.SERVICE.ExternalServices;
using ARSPlatform.SERVICE.Interfaces;
using ARSPlatform.SERVICE.Mapping;
using ARSPlatform.SERVICES;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
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

// Register External API Service
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();

// Register Audio Summary Service
// Timeout 15 phút để xử lý audio lớn
builder.Services.AddHttpClient<IAudioSummaryService, AudioSummaryService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaperService, PaperService>();

// Register PayOS Settings
builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOSSettings"));
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHttpClient();

// Register Email Settings and Service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

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
builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var keyString =
    jwtSettings["Key"]
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
    var approvedUserPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(
            "Researcher",
            "Admin",
            "Reviewer",
            "Lecturer",
            "Graduate Student")
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
            "Graduate Student");
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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "ARSPlatform API v1");
    });
}

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


app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();