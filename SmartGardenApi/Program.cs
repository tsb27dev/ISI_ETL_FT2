using System.Text;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Authentication.JwtBearer; // <--- Necessário (Instala o package se der erro)
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartGardenApi.Data;
using SmartGardenApi.Services;
using SmartGardenApi.Services.Soap;
using SQLitePCL;

// Initialize SQLite native provider
Batteries.Init();

var builder = WebApplication.CreateBuilder(args);

// --- SERVIÇOS ---

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddRouting(options => options.LowercaseUrls = true);

// HttpContextAccessor para serviços SOAP verificarem autenticação
builder.Services.AddHttpContextAccessor();

// Adiciona o AuthService
builder.Services.AddScoped<AuthService>();

// --- 1. SEGURANÇA PADRÃO DO .NET (NOVO) ---
var key = Encoding.UTF8.GetBytes(AuthService.SecretKey); // <--- UTF8 AQUI TAMBÉM

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero // Validade rigorosa
    };
});
// -------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Garden API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Cola o teu token assim (sem aspas): Bearer eyJhbGciOi...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
    c.DescribeAllParametersInCamelCase();
});

// SQLite (sem Entity Framework)
builder.Services.AddSingleton<SqliteGardenDb>();
builder.Services.AddScoped<IGardenRepository, SqliteGardenRepository>();
// User-Agent para o WeatherService não ser bloqueado
builder.Services.AddHttpClient<WeatherService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.68.0");
});

// SOAP
builder.Services.AddScoped<IPlantSoapService, PlantSoapService>();
builder.Services.AddScoped<PlantSoapService>();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

var app = builder.Build();

app.UsePathBase("/api");

// --- 2. CONFIGURAÇÃO DE PIPELINE (SEM MIDDLEWARE MANUAL) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// A ORDEM É CRÍTICA:
app.UseAuthentication(); // 1. Quem és tu?
app.UseAuthorization();  // 2. Tens permissão?

app.MapControllers();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<PlantSoapService>();
    serviceBuilder.AddServiceEndpoint<PlantSoapService, IPlantSoapService>(
        new BasicHttpBinding(), "/soap/plants");

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SqliteGardenDb>().EnsureCreatedAsync();
}

app.Run();