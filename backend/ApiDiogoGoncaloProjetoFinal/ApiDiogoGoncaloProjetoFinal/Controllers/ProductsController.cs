using ApiDiogoGoncaloProjetoFinal.Data;
using ApiDiogoGoncaloProjetoFinal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ApiDiogoGoncaloProjetoFinal.Controllers
{
    // --- 2. A Definição do Controlador ---
    [ApiController]
    [Route("api/[controller]")] // A URL para aceder a isto será: /api/products
    [Authorize]
    public class ProductsController : ControllerBase
    {
        // --- 3. A Ligação à Base de Dados ---
        // Vamos guardar uma referência ao nosso "tradutor" da BD (o DbContext)
        private readonly ApplicationDbContext _context;

        // O "Construtor": Quando o .NET cria este controlador,
        // ele "injeta" o DbContext que registámos no Program.cs
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 4. Os Endpoints (as "Ações") ---

        /// <summary>
        /// GET: /api/products
        /// Devolve uma lista de TODOS os produtos.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // Vai à BD, à tabela Products, e devolve-os todos como uma lista
            // Este é o endpoint que vai ter o CACHE (Redis + Polly) mais tarde
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        /// <summary>
        /// GET: /api/products/5
        /// Devolve UM produto específico pelo seu Id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            // Procura um produto na BD que tenha este Id
            var product = await _context.Products.FindAsync(id);

            // Se não encontrar (product for null), devolve um erro 404
            if (product == null)
            {
                return NotFound();
            }

            // Se encontrar, devolve o produto
            return Ok(product);
        }

        /// <summary>
        /// POST: /api/products
        /// Cria um novo produto na base de dados.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            // Adiciona o produto que veio no "body" do pedido
            _context.Products.Add(product);

            // Guarda as mudanças na base de dados
            await _context.SaveChangesAsync();

            // Devolve um status 201 (Created) e um link para o produto criado
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        /// <summary>
        /// PUT: /api/products/5
        /// Atualiza um produto existente.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            // Verifica se o Id da URL é o mesmo do Id do produto no "body"
            if (id != product.Id)
            {
                return BadRequest("O ID do URL não corresponde ao ID do produto.");
            }

            // Diz ao EF Core que este objeto (product) deve ser "seguido"
            // e marcado como "Modificado"
            _context.Entry(product).State = EntityState.Modified;

            try
            {
                // Tenta guardar as mudanças
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Se der erro (ex: alguém o apagou entretanto), verifica se ele
                // ainda existe. Se não, devolve 404.
                if (!_context.Products.Any(p => p.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw; // Deixa o erro maior acontecer
                }
            }

            // Devolve um status 204 (No Content) - significa "OK, atualizei."
            return NoContent();
        }

        /// <summary>
        /// DELETE: /api/products/5
        /// Apaga um produto da base de dados.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // Procura o produto na BD
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                // Se não o encontrar, devolve 404
                return NotFound();
            }

            // Se o encontrar, remove-o
            _context.Products.Remove(product);

            // E guarda as mudanças na BD
            await _context.SaveChangesAsync();

            // Devolve 204 (No Content)
            return NoContent();
        }
    }
}