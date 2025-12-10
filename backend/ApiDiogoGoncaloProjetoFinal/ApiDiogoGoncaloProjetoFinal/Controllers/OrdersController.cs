using ApiDiogoGoncaloProjetoFinal.Data;
using ApiDiogoGoncaloProjetoFinal.Models;
using ApiDiogoGoncaloProjetoFinal.DTOs; // Certifica-te que os DTOs (CreateOrderDto) estão aqui
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Necessário para ler o ID do Token

namespace ApiDiogoGoncaloProjetoFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // OBRIGATÓRIO: Só utilizadores com Login podem encomendar
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// POST: /api/orders
        /// Cria uma nova encomenda (Checkout).
        /// Recebe uma lista de produtos e quantidades.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            // --- 1. Descobrir QUEM está a comprar ---
            // Vamos ler o ID que está guardado dentro do Token JWT (claim "sub" ou NameIdentifier)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("Token inválido ou sem ID de utilizador.");
            }

            // Converter o ID de string para int
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest("ID de utilizador no token não é válido.");
            }

            // --- 2. Preparar o Cabeçalho da Encomenda (Tabela Orders) ---
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow, // Data de agora
                Status = "Pendente",         // Estado inicial
                TransactionId = Guid.NewGuid().ToString() // Simulamos um ID de pagamento único
            };

            // Adicionamos a encomenda à memória do EF (ainda não gravou na BD)
            _context.Orders.Add(order);

            // --- 3. Processar os Itens (Tabela OrderDetails) ---
            decimal totalAmount = 0;

            foreach (var itemDto in orderDto.Items)
            {
                // Verificar se o produto existe mesmo
                var product = await _context.Products.FindAsync(itemDto.ProductId);

                if (product == null)
                {
                    return BadRequest($"O produto com ID {itemDto.ProductId} não existe.");
                }

                // Criar o detalhe da encomenda
                var orderDetail = new OrderDetail
                {
                    // LIGAÇÃO IMPORTANTE: Dizemos que este detalhe pertence à 'order' que criámos acima
                    Order = order,

                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,

                    // IMPORTANTE: Guardamos o preço que está AGORA. 
                    // Se o preço mudar amanhã, o histórico desta compra não muda.
                    UnitPrice = product.Price
                };

                // Calcular o total só para mostrar na resposta
                totalAmount += (product.Price * itemDto.Quantity);

                // Adicionar este detalhe à memória do EF
                _context.OrderDetails.Add(orderDetail);
            }

            // --- 4. Gravar TUDO na Base de Dados ---
            // O SaveChanges é inteligente: vai criar primeiro a Order, gerar o ID automático,
            // e depois usar esse ID para criar os OrderDetails. Tudo numa transação.
            await _context.SaveChangesAsync();

            // --- 5. Responder ao Cliente ---
            return Ok(new
            {
                Message = "Encomenda criada com sucesso!",
                OrderId = order.Id,
                TotalValue = totalAmount,
                Status = order.Status,
                Date = order.OrderDate
            });
        }

        /// <summary>
        /// GET: /api/orders
        /// Vê o histórico de encomendas APENAS do utilizador que está logado.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetMyOrders()
        {
            // 1. Ler o ID do utilizador do token novamente
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            // 2. Buscar à BD apenas as encomendas deste ID
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)      // Filtro de segurança
                .OrderByDescending(o => o.OrderDate) // Mais recentes primeiro
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    o.TransactionId
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}