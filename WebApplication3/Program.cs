using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using AuctionHouse.API.Data;
using AuctionHouse.API.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configure detailed logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Set logging levels for debugging
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Logging.AddFilter("AuctionHouse.API", LogLevel.Debug);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Debug);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure CORS for frontend applications (React/Next.js)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",    // Next.js default
                "https://localhost:3000",   // Next.js HTTPS
                "http://localhost:3001",    // Alternative port
                "https://localhost:3001",   // Alternative port HTTPS
                "http://127.0.0.1:3000",   // IP alternative
                "https://127.0.0.1:3000",  // IP alternative HTTPS
                "http://localhost:5000",    // Backend HTTP (for same-origin testing)
                "https://localhost:7000"    // Backend HTTPS (for same-origin testing)
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(2520)); // Cache preflight for 42 minutes
    });

    // Add a more permissive policy for development
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];

if (string.IsNullOrEmpty(secret))
{
    throw new InvalidOperationException("JWT Secret is not configured in appsettings.json");
}

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero, // Remove default 5 minute clock skew
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
    
    // Map JWT claims to ClaimTypes
    options.MapInboundClaims = false; // Keep original claim names
    
    // Add event handlers for debugging
    if (builder.Environment.IsDevelopment())
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("JWT Token validated successfully for user: {UserId}", 
                    context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    }
});

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IBiddingService, BiddingService>();
builder.Services.AddScoped<WebApplication3.Services.IImageService, WebApplication3.Services.ImageService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Seed the database in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            
            // Ensure database is created and up to date
            await context.Database.MigrateAsync();
            
            // Seed sample data
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// For development, you might want to comment out HTTPS redirection
// if having certificate issues - HTTPS redirection is now disabled for development
// Uncomment the line below if you want to force HTTPS in production
// if (!app.Environment.IsDevelopment())
// {
//     app.UseHttpsRedirection();
// }

// Enable CORS - MUST come before UseAuthentication and UseAuthorization
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "AllowFrontend");

// Add middleware for handling OPTIONS requests (preflight)
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        return;
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Configure static file serving for uploaded images
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(Path.Combine(uploadsPath, "auctions"));

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.MapControllers();

// Add a test endpoint to verify the API is working
app.MapGet("/api/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    urls = new { 
        http = "http://localhost:5000", 
        https = "https://localhost:7000" 
    }
});

// Add CORS test endpoint
app.MapGet("/api/cors-test", (HttpContext context) => new {
    origin = context.Request.Headers["Origin"].ToString(),
    method = context.Request.Method,
    headers = context.Request.Headers.Keys.ToArray(),
    message = "CORS is working!"
});

app.Run();