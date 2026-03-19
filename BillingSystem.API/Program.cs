using BillingSystem.API.Extensions;
using BillingSystem.API.Middleware;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocs();

// Updated CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
            "http://localhost:4200",
            "https://billing-system-frontend-nu.vercel.app",
            "https://billing-system-frontend-two.vercel.app" // Your NEW Vercel URL
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()); // Added for better compatibility with login tokens
});

QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// --- DATABASE AUTO-MIGRATE ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// --- MIDDLEWARE PIPELINE (ORDER IS CRITICAL) ---

app.UseMiddleware<ExceptionMiddleware>();

// Enable Swagger in Production so you can test on Railway
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing System API v1");
        c.RoutePrefix = "swagger"; 
    });
}

// 1. Routing must be near the top
app.UseRouting();

// 2. CORS MUST be before Authentication/Authorization
app.UseCors("AllowAll");

// 3. Security Middlewares
app.UseAuthentication();
app.UseAuthorization();

// 4. Map the endpoints
app.MapControllers();

app.Run();