using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApplication1.Auth;
using WebApplication1.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MinimalApiContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"),
    sqlServerOptions => sqlServerOptions.CommandTimeout((int)TimeSpan.FromMinutes(30).TotalSeconds));
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "MyAPI", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});
builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
}
app.GenerateJwtToken();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


app.MapGet("/GetAll", async (MinimalApiContext dbContext) =>
{
    var employees = await dbContext.Students.ToListAsync();
    if (employees == null)
    {
        return Results.NoContent();
    }
    return Results.Ok(employees);
}).RequireAuthorization();
app.MapGet("GetById/{id}", async (int id, MinimalApiContext dbcontext) =>
{
    Student emp = await dbcontext.Students.Where(e => e.StudentId == id).FirstOrDefaultAsync();
    if (id == 0 && id < 0)
    {
        return Results.NoContent();
    }
    return Results.Ok(emp);
}).RequireAuthorization();
app.MapPost("PostIteam", async ([FromBody] Student emp, [FromServices] MinimalApiContext dbcontext) =>
{
    await dbcontext.Students.AddAsync(emp);
    await dbcontext.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();
app.MapPut("updateIteam/{id}", async (int id, Student emp, MinimalApiContext dbcontext) =>
{
    var data = await dbcontext.Students.FindAsync(id);
    if (data == null)
    {
        return Results.NoContent();
    }
    data.FirstName = emp.FirstName;
    data.LastName = emp.LastName;
    data.Birthdate = emp.Birthdate;
    data.Email = emp.Email;
    data.Phone = emp.Phone;
    await dbcontext.SaveChangesAsync();
    return Results.Ok(data);
}).RequireAuthorization();
app.MapDelete("Delete/{id}", async (int id, MinimalApiContext dbContext) =>
{
    var iteam = await dbContext.Students.FindAsync(id);
    if (iteam == null)
    {
        return Results.NoContent();
    }
    dbContext.Students.Remove(iteam);
    await dbContext.SaveChangesAsync();
    return Results.Ok(iteam);
}).RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();
app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
