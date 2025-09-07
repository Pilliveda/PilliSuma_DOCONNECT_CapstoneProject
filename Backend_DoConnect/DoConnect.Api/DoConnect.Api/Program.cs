
using System.Text;
using DoConnect.Api.Data;
using DoConnect.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddControllers();

// Needed by Swagger..
builder.Services.AddEndpointsApiExplorer();

// ---------- Swagger (with JWT “Authorize”) ----------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DoConnect API",
        Version = "v1",
        Description = "Q&A platform (Angular + ASP.NET Core)"
    });

    // Bearer auth
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste your JWT here (no 'Bearer ' prefix).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });

    // (Optional) include XML doc comments if you enable them in the .csproj
    var xml = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});
// ----------------------------------------------------

// EF Core
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=VEDA\\SQLEXPRESS;Initial Catalog=DoConnectFullStackDb;Integrated Security=True;Persist Security Info=False;Pooling=False;Multiple Active Result Sets=False;Encrypt=True;Trust Server Certificate=True;Application Name=\"SQL Server Management Studio\";Command Timeout=0";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connStr));

// JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromSeconds(5),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

// CORS for Angular
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ng", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ImageStorageService>();

var app = builder.Build();

app.MapGet("/", () => Results.Json(new { message = "API is running." }));

// ---------- Enable Swagger UI ----------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DoConnect API v1");
    c.RoutePrefix = "swagger"; // browse at /swagger
});
// --------------------------------------

app.UseStaticFiles(); // serve wwwroot/uploads
app.UseCors("ng");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (!db.Users.Any(u => u.Role == DoConnect.Api.Models.RoleType.Admin))
    {
        db.Users.Add(new DoConnect.Api.Models.User
        {
            Username = "admin",
            Email = "admin@doconnect.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = DoConnect.Api.Models.RoleType.Admin
        });
        await db.SaveChangesAsync();
    }
}

app.Run();
