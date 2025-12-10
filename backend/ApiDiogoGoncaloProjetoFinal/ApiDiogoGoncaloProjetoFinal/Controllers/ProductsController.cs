using ApiDiogoGoncaloProjetoFinal.Data;
using ApiDiogoGoncaloProjetoFinal.Models;
using ApiDiogoGoncaloProjetoFinal.DTOs; // --- NOVO: Para usar o SupplierStockDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed; // Para falar com o Redis
using System.Text.Json; // Para converter os produtos em Texto (JSON) e vice-versa

namespace ApiDiogoGoncaloProjetoFinal.Controllers
{
    // --- 2. A Definição do Controlador ---
    [ApiController]
    [Route("api/[controller]")] // A URL para aceder a isto será: /api/products
    [Authorize]
    public class ProductsController : ControllerBase
    {
        // --- 3. A Ligação à Base de Dados, Cache e Serviços Externos ---

        // Vamos guardar uma referência ao nosso "tradutor" da BD (o DbContext)
        private readonly ApplicationDbContext _context;

        // Vamos guardar uma referência ao nosso Cache (Redis)
        private readonly IDistributedCache _cache;

        // --- NOVO: A "Fábrica" que cria clientes HTTP para falar com outras APIs (WireMock) ---
        private readonly IHttpClientFactory _httpClientFactory;

        // O "Construtor": Quando o .NET cria este controlador,
        // ele "injeta" as dependências que registámos no Program.cs
        public ProductsController(ApplicationDbContext context, IDistributedCache cache, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _cache = cache; // Guardamos o Redis pronto a usar
            _httpClientFactory = httpClientFactory; // --- NOVO: Guardamos a fábrica pronta a usar
        }

        // --- 4. Os Endpoints (as "Ações") ---

        /// <summary>
        /// GET: /api/products
        /// Devolve uma lista de TODOS os produtos.
        /// AGORA COM REDIS CACHE!
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // Definimos uma "chave" única para guardar esta lista no Redis
            string cacheKey = "lista_todos_produtos";

            // --- PASSO 1: Tentar ir buscar ao Redis ---
            // Perguntamos ao Redis: "Já tens a lista 'lista_todos_produtos'?"
            string? cachedProducts = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProducts))
            {
                // SE ENCONTRAR (Cache Hit):
                // O Redis devolve texto (JSON), temos de converter de volta para Lista de Produtos
                // e devolvemos logo daqui. A BD nem sequer é incomodada!
                var productsFromCache = JsonSerializer.Deserialize<List<Product>>(cachedProducts);
                return Ok(productsFromCache);
            }

            // --- PASSO 2: Se não houver no Redis (Cache Miss) ---

            // Vai à BD (MySQL), à tabela Products, buscar tudo
            var productsFromDb = await _context.Products.ToListAsync();

            // --- PASSO 3: Guardar no Redis para a próxima vez ---

            // Configurar opções: O cache expira passados 2 minutos (para não ficar velho para sempre)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            };

            // Converter a lista da BD para texto JSON
            string jsonToCache = JsonSerializer.Serialize(productsFromDb);

            // Gravar no Redis
            await _cache.SetStringAsync(cacheKey, jsonToCache, cacheOptions);

            return Ok(productsFromDb);
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

        // --- NOVO ENDPOINT DE DROPSHIPPING (Integração com WireMock) ---
        /// <summary>
        /// GET: /api/products/1/stock
        /// 1. Vai à BD buscar o SKU do produto.
        /// 2. Vai ao Fornecedor (WireMock) perguntar o stock real.
        /// </summary>
        [HttpGet("{id}/stock")]
        public async Task<IActionResult> GetProductStock(int id)
        {
            // 1. Ir à BD local apenas para saber qual é o SKU deste produto
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produto não encontrado na BD local.");

            // 2. Preparar o pedido ao Fornecedor
            // Pedimos à fábrica para nos dar o cliente "FornecedorClient" que configurámos no Program.cs
            // Este cliente já tem o endereço base do WireMock e a Resiliência (Polly) configurada.
            var client = _httpClientFactory.CreateClient("FornecedorClient");

            try
            {
                // 3. Fazer a chamada ao WireMock
                // URL Final será: http://wiremock-fornecedor:8080/inventory/{SKU}
                var response = await client.GetFromJsonAsync<SupplierStockDto>($"inventory/{product.Sku}");

                if (response == null) return NotFound("O fornecedor não devolveu dados.");

                // 4. Devolver uma resposta bonita ao cliente, combinando dados da BD e do Fornecedor
                return Ok(new
                {
                    Produto = product.Name,
                    Descricao = product.Name, // ou product.Description se tiveres
                    Sku = product.Sku,
                    PrecoLoja = product.Price,
                    // Dados vindos do WireMock:
                    StockNoFornecedor = response.StockQuantity,
                    EnvioEstimado = response.ExpectedShipping,
                    Fornecedor = response.SupplierName
                });
            }
            catch (Exception ex)
            {
                // Se o WireMock estiver desligado, ou der erro 3 vezes seguidas (Polly desistiu),
                // caímos aqui. Devolvemos 503 (Service Unavailable).
                return StatusCode(503, $"Não foi possível contactar o fornecedor: {ex.Message}");
            }
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

            // --- INVALIDAR O CACHE ---
            // Como adicionámos um produto novo, a lista que está no Redis está velha!
            // Temos de a apagar para obrigar o próximo GET a ir buscar a lista atualizada.
            await _cache.RemoveAsync("lista_todos_produtos");

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

                // --- INVALIDAR O CACHE ---
                // Mudámos um produto, logo a lista no cache pode estar errada. Apagamos.
                await _cache.RemoveAsync("lista_todos_produtos");
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

            // --- INVALIDAR O CACHE ---
            // Apagámos um produto, a lista no cache tem um produto a mais. Apagamos.
            await _cache.RemoveAsync("lista_todos_produtos");

            // Devolve 204 (No Content)
            return NoContent();
        }
    }
}