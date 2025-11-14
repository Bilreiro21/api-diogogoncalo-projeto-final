// --- 1. Os "Usings" que precisamos ---
using ApiDiogoGoncaloProjetoFinal.Data;    // Onde está o ApplicationDbContext
using ApiDiogoGoncaloProjetoFinal.DTOs;     // Onde estão os nossos DTOs
using ApiDiogoGoncaloProjetoFinal.Models;   // Onde está o User
using Microsoft.AspNetCore.Identity;      // Para o PasswordHasher
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;     // Para o JWT
using System.IdentityModel.Tokens.Jwt;    // Para o JWT
using System.Security.Claims;             // Para o JWT
using System.Text;                        // Para a chave do JWT

namespace ApiDiogoGoncaloProjetoFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // A URL será /api/auth
    public class AuthController : ControllerBase
    {
        // --- 2. As "Ferramentas" que vamos usar ---
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

        // --- 3. O Endpoint de Registo ---
        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // 1. Verificar se o email já existe
            var userExists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email);
            if (userExists)
            {
                return BadRequest(new { Message = "Este email já está a ser utilizado." });
            }

            // 2. Criar o novo objeto User
            var user = new User
            {
                Email = registerDto.Email,
                Name = null, // O nosso DTO não pede o nome, por isso fica null
                RegistrationDate = DateTime.UtcNow // O nosso SQL já faz isto, mas é boa prática
            };

            // 3. CRIPTOGRAFAR a password (NUNCA guardar a password em texto)
            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            // 4. Guardar o utilizador na BD
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Utilizador registado com sucesso!" });
        }

        // --- 4. O Endpoint de Login ---
        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 1. Encontrar o utilizador pelo email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // 2. Verificar se o utilizador existe E se a password está correta
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password) == PasswordVerificationResult.Failed)
            {
                // Damos uma resposta vaga de propósito (por segurança)
                return Unauthorized(new { Message = "Email ou password inválidos." });
            }

            // 3. Se tudo estiver correto, GERAR O TOKEN JWT
            var token = GenerateJwtToken(user);

            // 4. Devolver o token ao cliente
            return Ok(new { Token = token });
        }


        // --- 5. O Método Privado que GERA O TOKEN ---
        private string GenerateJwtToken(User user)
        {
            // 1. A "Chave Secreta" (temos de a pôr no appsettings.json)
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("A chave JWT (Jwt:Key) não está configurada no appsettings.json");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 2. Os "Claims" (informação que vai dentro do token)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // O Id do utilizador
                new Claim(JwtRegisteredClaimNames.Email, user.Email),       // O Email
                new Claim(ClaimTypes.Name, user.Email)                      // O Nome (usamos o email)
            };

            // 3. O "Emissor" e "Audiência" (quem o fez, para quem é)
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            // 4. Criar o token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(2), // O token expira em 2 horas
                signingCredentials: creds
            );

            // 5. Devolver o token como uma string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}