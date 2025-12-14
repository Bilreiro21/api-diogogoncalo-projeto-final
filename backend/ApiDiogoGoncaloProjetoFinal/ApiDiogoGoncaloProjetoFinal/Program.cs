using ApiDiogoGoncaloProjetoFinal.Data;
using ApiDiogoGoncaloProjetoFinal.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity; // Para o PasswordHasher
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text; // Para o Encoding.UTF8
using Microsoft.OpenApi.Models;
using Polly; // Necessário para as políticas de resiliência

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Lê a connection string do appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(ServerVersion.AutoDetect(connectionString));

// Regista o DbContext e diz-lhe para usar MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, serverVersion));

// config do redis cache (Recuperado)
// O ProductsController precisa disto, senão dá erro ao iniciar!
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Lê a string de conexão "Redis" do docker-compose ou appsettings
    // Se não estiver configurada, assume localhost:6379
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.Configuration = redisConnection;
    options.InstanceName = "ProjetoFinal_";
});

// CONFIGURAÇÃO DO CLIENTE HTTP PARA O WIREMOCK (NOVO)
// Criamos um cliente chamado "FornecedorClient" que sabe falar com o Fornecedor Falso
builder.Services.AddHttpClient("FornecedorClient", client =>
{
    // aqui usamos o NOME DO SERVIÇO no Docker (wiremock-fornecedor).
    // o Docker resolve este nome para o IP correto do contentor.
    client.BaseAddress = new Uri("http://localhost:9090/");
})
// Adiciona uma política de resiliência (Polly):
// Se o pedido falhar (ex: erro de rede ou 5xx), tenta de novo automaticamente.
// Tenta 3 vezes, esperando 500ms entre cada tentativa.
.AddTransientHttpErrorPolicy(policy =>
    policy.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)));


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adiciona uma definição de segurança (o que é "Bearer")
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduza 'Bearer' [espaço] e o seu token. \n\nExemplo: 'Bearer eyJhbGciOiJIUzI1Ni...'"
    });

    // Diz ao Swagger para APLICAR esta definição a todos os endpoints
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

// Regista o IPasswordHasher para o AuthController o poder usar
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Configuração do JWT

// diz à API que vamos usar Autenticação
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

// ensinar a API a validar o "Bearer" Token (o JWT)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // O que validar:
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // rejeita tokens expirados
            ValidateIssuerSigningKey = true,

            // Quais os valores válidos (lidos do appsettings.json):
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!) // "!" diz que temos a certeza que a Key existe
            )
        };
    });

// adicionamos isto também para o [Authorize] funcionar
builder.Services.AddAuthorization();

// adicionar o Serviço de CORS (Permitir tudo para desenvolvimento)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // permite qualquer site 
              .AllowAnyMethod()   // permite GET, POST, PUT, DELETE
              .AllowAnyHeader();  // permite enviar o Token JWT
    });
});

var app = builder.Build();

// configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Ativar o CORS
app.UseCors("AllowAll");

app.UseAuthentication(); // quem és tu?
app.UseAuthorization();  // o que podes fazer?

app.MapControllers();

app.Run();