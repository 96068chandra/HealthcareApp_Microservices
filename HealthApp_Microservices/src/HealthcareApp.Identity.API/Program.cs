using HealthcareApp.Common.Domain.Interfaces;
using HealthcareApp.Common.Infrastructure.Data;

using HealthcareApp.Identity.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure SQL Server
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("HealthcareApp.Identity.API")));


// Register repository for ApplicationUser with correct DbContext
builder.Services.AddScoped<IBaseRepository<HealthcareApp.Identity.API.Models.Entities.ApplicationUser>>(sp =>
    new BaseRepository<HealthcareApp.Identity.API.Models.Entities.ApplicationUser>(sp.GetRequiredService<IdentityDbContext>()));

// Register password hasher
builder.Services.AddScoped<IPasswordHasher<HealthcareApp.Identity.API.Models.Entities.ApplicationUser>, PasswordHasher<HealthcareApp.Identity.API.Models.Entities.ApplicationUser>>();

// Register JWT token service
builder.Services.AddScoped<HealthcareApp.Identity.API.Services.ITokenService, HealthcareApp.Identity.API.Services.TokenService>();

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.Migrate();
    }
}

app.Run();
