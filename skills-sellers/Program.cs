using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Models;
using skills_sellers.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// ssl security
builder.WebHost.UseUrls("https://*:5002");

// cors
services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        Console.Out.WriteLine("Adding cors policy");
        builder.WithOrigins("http://localhost:5173", "http://skills-sellers.team-unc.fr") // Ajoutez votre domaine client ici
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Important pour SignalR
    });
});

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
services.AddScoped<ICardService, CardService>();
services.AddScoped<IAuthService, AuthService>();

// DbContext
services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));


// Authentication
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                configuration.GetSection("jwt")["secret"]))
    };
});



// documentation
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillsSellers", Version = "v1" });
    c.AddSignalRSwaggerGen();
    c.CustomSchemaIds(x => x.FullName); // Enables to support different classes with the same name using the full name with namespace
    c.SchemaFilter<NamespaceSchemaFilter>();
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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


/// <summary>
/// Apply filter name for swagger
/// </summary>
public class NamespaceSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is null)
            throw new ArgumentNullException(nameof(schema));
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        schema.Title = context.Type.Name; // To replace the full name with namespace with the class name only
    }
}
