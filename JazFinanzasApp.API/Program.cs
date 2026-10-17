using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Middleware;
using JazFinanzasApp.API.Infrastructure;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Infrastructure.Repositories;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Conexi�n a la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("JazFinanzasAppConnectionString"),
        sqlOptions => sqlOptions.CommandTimeout(120));
});

// Registrar los repositorios y los servicios (ver Infrastructure/ApplicationServiceRegistration.cs)
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddIdentityCore<User>()
    .AddRoles<IdentityRole<int>>()
    .AddTokenProvider<DataProtectorTokenProvider<User>>("JazFinanzasApp")
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
});

// Configuraci�n de JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            AuthenticationType = "Jwt",
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = "role"
        };
    });

// Configuraci�n de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",  // Dominio de desarrollo
            "https://jazfinanzaswebapp.azurestaticapps.net" // Dominio de producci�n
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Rate limiting para login
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", cfg =>
    {
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.PermitLimit = 10;
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Aplica la pol�tica de CORS
app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Apply pending database migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error applying database migrations.");
}

// Seed: crear rol Admin y asignarlo al usuario administrador
try
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole<int>("Admin"));

    var adminUser = await userManager.FindByNameAsync("ajazmatie");
    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Seed de roles no pudo ejecutarse. Verifique la conexión a la base de datos.");
}

app.Run();