using ApiDiogoGoncaloProjetoFinal.Data;
using ApiDiogoGoncaloProjetoFinal.DTOs;
using ApiDiogoGoncaloProjetoFinal.Models;   // Onde está o User
using Microsoft.AspNetCore.Identity;      // Para o PasswordHasher
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;             // Para o JWT
using System.Text;                        // Para a chave do JWT

namespace ApiDiogoGoncaloProjetoFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // A URL será /api/auth
    public class AuthController : ControllerBase
    {
        // As "Ferramentas" que vamos usar
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        // "Injetamos" o DbContext, o Hasher e a Configuração
        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        // Endpoint de Registo
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Verificar se o email já existe
            var userExists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email);
            if (userExists)
            {
                return BadRequest(new { Message = "Este email já está a ser utilizado." });
            }

            // Criar o novo objeto User
            var user = new User
            {
                Email = registerDto.Email,
                Name = null, // O nosso DTO não pede o nome, por isso fica null
                RegistrationDate = DateTime.UtcNow // O nosso SQL já faz isto, mas é boa prática
            };

            // CRIPTOGRAFAR a password
            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            // Guardar o utilizador na BD
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Utilizador registado com sucesso!" });
        }

        // Endpoint de Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Encontrar o utilizador pelo email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // Verificar se o utilizador existe e se a password está correta
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password) == PasswordVerificationResult.Failed)
            {
                // Damos uma resposta vaga de propósito (por segurança)
                return Unauthorized(new { Message = "Email ou password inválidos." });
            }

            // Se tudo estiver correto, GERAR O TOKEN JWT
            var token = GenerateJwtToken(user);

            // Devolver o token ao cliente
            return Ok(new { Token = token });
        }


        // O Método Privado que GERA O TOKEN
        private string GenerateJwtToken(User user)
        {
            // A "Chave Secreta" (temos de a pôr no appsettings.json)
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("A chave JWT (Jwt:Key) não está configurada no appsettings.json");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Os "Claims" (informação que vai dentro do token)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Email) // O Nome (usamos o email)
            };

            // O "Emissor" e "Audiência" (quem o fez, para quem é)
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            // Criar o token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(2), // O token expira em 2 horas
                signingCredentials: creds
            );

            // Devolver o token como uma string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}