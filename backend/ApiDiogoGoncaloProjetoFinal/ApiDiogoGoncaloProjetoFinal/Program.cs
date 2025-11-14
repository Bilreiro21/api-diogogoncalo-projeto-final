using ApiDiogoGoncaloProjetoFinal.Data;
using ApiDiogoGoncaloProjetoFinal.Models; // Onde está o seu modelo User
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity; // Para o PasswordHasher
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text; // Para o Encoding.UTF8
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Lê a connection string do appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(ServerVersion.AutoDetect(connectionString));

// Regista o DbContext (o nosso "tradutor") e diz-lhe para usar MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, serverVersion));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. Adiciona uma definição de segurança (o que é "Bearer")
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduza 'Bearer' [espaço] e o seu token. \n\nExemplo: 'Bearer eyJhbGciOiJIUzI1Ni...'"
    });

    // 2. Diz ao Swagger para APLICAR esta definição a todos os endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ... (depois de builder.Services.AddDbContext)

// Regista o IPasswordHasher para o AuthController o poder usar
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// --- Configuração do JWT ---

// 1. Diz à API que vamos usar Autenticação
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })

// 2. Ensina a API a validar o "Bearer" Token (o JWT)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // O que validar:
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // Rejeita tokens expirados
            ValidateIssuerSigningKey = true, // A parte mais importante

            // Quais os valores válidos (lidos do appsettings.json):
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!) // "!" diz que temos a certeza que a Key existe
            )
        };
    });

// Temos de adicionar isto também para o [Authorize] funcionar
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Quem és tu?
app.UseAuthorization();  // O que podes fazer?

app.MapControllers();

app.Run();
