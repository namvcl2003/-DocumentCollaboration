using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DocumentCollaboration.Application.Services;
using DocumentCollaboration.Domain.Interfaces;
using DocumentCollaboration.Infrastructure.Data;
using DocumentCollaboration.Infrastructure.Repositories;
using DocumentCollaboration.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== SERILOG CONFIGURATION =====
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ===== DATABASE CONFIGURATION =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    )
);

// ===== REPOSITORY PATTERN =====
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ===== APPLICATION SERVICES =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();

// ===== JWT AUTHENTICATION =====
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? throw new InvalidOperationException("JWT Secret is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DocumentCollaboration";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DocumentCollaborationUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Error($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information($"Token validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Quản trị viên"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Trưởng phòng", "Quản trị viên"));
    options.AddPolicy("RequireViceManager", policy => policy.RequireRole("Phó phòng", "Trưởng phòng", "Quản trị viên"));
});

// ===== CORS CONFIGURATION =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ===== CONTROLLERS =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ===== API EXPLORER & SWAGGER =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Document Collaboration API",
        Version = "v1",
        Description = "API for Document Collaboration System with 3-Level Workflow Management",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@company.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===== HTTP CONTEXT ACCESSOR =====
builder.Services.AddHttpContextAccessor();

// ===== MEMORY CACHE =====
builder.Services.AddMemoryCache();

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Collaboration API v1");
        options.RoutePrefix = string.Empty;
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Security headers
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// CORS
app.UseCors("AllowReactApp");

// Static files
app.UseStaticFiles();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health check
app.MapHealthChecks("/health");

// Error handler
app.MapGet("/error", () => Results.Problem("An error occurred while processing your request."))
    .ExcludeFromDescription();

// Welcome endpoint
app.MapGet("/", () => new
{
    ApplicationName = "Document Collaboration API",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    SwaggerUrl = "/swagger",
    Features = new[]
    {
        "3-Level Workflow (Assistant → Vice Manager → Manager)",
        "JWT Authentication & Authorization",
        "Document Version Control",
        "Comment System",
        "Notification System",
        "Audit Logging",
        "File Storage Management"
    }
}).ExcludeFromDescription();

// ===== DATABASE CONNECTION TEST =====
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        if (dbContext.Database.CanConnect())
        {
            Log.Information("✅ Database connection successful");
            
            // Check if we have data
            var userCount = dbContext.Users.Count();
            var roleCount = dbContext.Roles.Count();
            var deptCount = dbContext.Departments.Count();
            
            Log.Information($"📊 Database statistics: {userCount} users, {roleCount} roles, {deptCount} departments");
            
            if (userCount == 0)
            {
                Log.Warning("⚠️  No users found in database. Please run SeedData.sql");
            }
        }
        else
        {
            Log.Warning("❌ Cannot connect to database. Please check connection string.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error connecting to database");
    }
}

Log.Information("🚀 Starting Document Collaboration API");
Log.Information("📝 Swagger UI available at: {SwaggerUrl}", app.Environment.IsDevelopment() ? "http://localhost:5057/swagger" : "/swagger");

try
{
    app.Run();
    Log.Information("✅ Application started successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}