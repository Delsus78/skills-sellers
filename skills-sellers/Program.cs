using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Helpers.Bdd.Contexts;
using skills_sellers.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// ssl security
builder.WebHost.UseUrls("https://*:5002");

// Add services to the container.
services.AddControllers(options =>
{
    options.Filters.Add<AppExceptionFiltersAttribute>();
});
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddSignalR();

// add services
services.AddScoped<IUserService, UserService>();
services.AddDbContext<UserContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));


// Authentication
services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

// documentation
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillsSellers", Version = "v1" });
    c.AddSignalRSwaggerGen();
});

// configurations


// build
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();