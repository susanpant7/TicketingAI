using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketingAI.Api.Auth;
using TicketingAI.Api.Chat;
using TicketingAI.Api.Common;
using TicketingAI.Api.Departments;
using TicketingAI.Api.Knowledge;
using TicketingAI.Api.Settings;
using TicketingAI.Api.Tickets;
using TicketingAI.Api.Users;
using TicketingAI.Data.Persistence;

DotEnv.LoadForDevelopment();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// A client hosted on another address (for example Vercel) needs its address here: Cors:AllowedOrigins, separated by commas.
// With no value, CORS stays off, which is right when the page and the API share an address (Vite dev server, nginx).
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (allowedOrigins.Length > 0)
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
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
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<DataCleanup>();
// Ollama's address comes from a setting (ollama.base_url), so the clients have no base address.
builder.Services.AddHttpClient<IEmbeddingClient, OllamaEmbeddingClient>(client => client.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddOptions<ChatApiOptions>().BindConfiguration(ChatApiOptions.SectionName);
builder.Services.AddOptions<AiDefaultsOptions>().BindConfiguration(AiDefaultsOptions.SectionName);
// A 7B model on a CPU can take a while to read the sections and write the answer.
builder.Services.AddHttpClient<OllamaChatClient>(client => client.Timeout = TimeSpan.FromMinutes(10));
// An external chat service (Gemini, Cloudflare, ...): its address comes from a setting, so the client has no base address.
builder.Services.AddHttpClient<ApiChatClient>(client => client.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddScoped<IChatModelClient, ChatModelClient>();
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

    // A new database starts with the Ollama address and the chat provider, address and model from the configuration (AiDefaults), if they are set.
    await SettingsSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IOptions<AiDefaultsOptions>>().Value.ToOverrides());
}

if (allowedOrigins.Length > 0)
    app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
