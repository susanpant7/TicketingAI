using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketingAI.Api.Auth;
using TicketingAI.Api.Chat;
using TicketingAI.Api.Departments;
using TicketingAI.Api.Knowledge;
using TicketingAI.Api.Settings;
using TicketingAI.Api.Users;
using TicketingAI.Data.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(new SqliteVecConnectionInterceptor()));

builder.Services.AddOptionsWithValidateOnStart<AdminOptions>()
    .BindConfiguration(AdminOptions.SectionName)
    .ValidateDataAnnotations();
builder.Services.AddOptionsWithValidateOnStart<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddOptions<OllamaOptions>().BindConfiguration(OllamaOptions.SectionName).ValidateDataAnnotations();
builder.Services.AddHttpClient<IEmbeddingClient, OllamaEmbeddingClient>((services, client) =>
{
    client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient<IChatModelClient, OllamaChatClient>((services, client) =>
{
    client.BaseAddress = new Uri(services.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl);
    // A 7B model on a CPU can take a while to read the sections and write the answer.
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();

Directory.CreateDirectory("data");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var vecVersion = db.Database.SqlQueryRaw<string>("SELECT vec_version() AS Value").Single();
    app.Logger.LogInformation("sqlite-vec {Version} loaded", vecVersion);

    await SettingsSeeder.SeedAsync(db);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
