using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using skills_sellers.Entities.Actions;
using skills_sellers.Helpers;
using skills_sellers.Helpers.Bdd;
using skills_sellers.Hubs;
using skills_sellers.Services;
using skills_sellers.Services.ActionServices;
using skills_sellers.Services.GameServices;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// ssl security
builder.WebHost.UseUrls("http://*:5002");

builder.Logging.AddConsole();

// cors
services.AddCors(options =>
{
    options.AddDefaultPolicy(corsPolicyBuilder =>
    {
        Console.Out.WriteLine("Adding cors policy");
        corsPolicyBuilder.WithOrigins("http://localhost:5173", "https://skills-sellers.fr")
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
services.AddMemoryCache();
services.AddResponseCaching();

// add services
services.AddScoped<IUserService, UserService>();
services.AddScoped<ICardService, CardService>();
services.AddScoped<IWeaponService, WeaponService>();
services.AddScoped<ICosmeticService, CosmeticService>();
services.AddScoped<IRegistreService, RegistreService>();
services.AddScoped<ISeasonService, SeasonService>();
services.AddScoped<IWarService, WarService>();
services.AddScoped<IAuthService, AuthService>();
services.AddSingleton<IStatsService, StatsService>();
services.AddScoped<IAchievementsService, AchievementsService>();
services.AddScoped<IMarchandService, MarchandService>();
services.AddScoped<IResourcesService, ResourcesService>();
services.AddScoped<IUserBatimentsService, UserBatimentsService>();
services.AddSingleton<INotificationService, NotificationService>();
services.AddSingleton<IRegistrationLinkCreatorService, RegistrationLinkCreatorService>();

// add action services
services.AddSingleton<IActionTaskService, ActionTaskService>();
services.AddScoped<ExplorerActionService>();
services.AddScoped<AmeliorerActionService>();
services.AddScoped<CuisinerActionService>();
services.AddScoped<MusclerActionService>();
services.AddScoped<ReparerActionService>();
services.AddScoped<SatelliteActionService>();
services.AddScoped<WarActionService>();
services.AddScoped<BossActionService>();

// add games services
services.AddScoped<GamesService>();
services.AddScoped<CasinoService>();
services.AddScoped<MachineRepairService>();
services.AddScoped<WordleGameService>();
services.AddScoped<BossService>();
services.AddSingleton<BlackJackService>();

// special services
services.AddScoped<IChristmasService, ChristmasService>();

// add daily task service
services.AddScoped<IDailyTaskService, DailyTaskService>();
services.AddScoped<IHostileRegistreAttackService, HostileRegistreAttackService>();

// hosted services
services.AddHostedService<HostedStatsService>();
services.AddHostedService<HostedTasksService>();
services.AddHostedService<DailyTaskHostedService>();
services.AddHostedService<HostileRegistreHostedService>();

// DbContext
services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
                configuration.GetSection("jwt")["secret"] ?? throw new InvalidOperationException("No jwt secret found")))
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/globalChatHub") || path.StartsWithSegments("/notificationHub") || path.StartsWithSegments("/blackJackHub")))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});



// documentation
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillsSellers", Version = "v1" });
    c.AddSignalRSwaggerGen();
    c.CustomSchemaIds(x => x.FullName); // Enables to support different classes with the same name using the full name with namespace
    c.SchemaFilter<NamespaceSchemaFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    
// configurations


// build
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GlobalChatHub>("/globalChatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<BlackJackHub>("/blackJackHub");

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
