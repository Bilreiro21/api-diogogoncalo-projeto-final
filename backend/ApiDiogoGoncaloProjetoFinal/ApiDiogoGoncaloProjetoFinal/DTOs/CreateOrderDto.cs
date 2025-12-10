using System.ComponentModel.DataAnnotations;

namespace ApiDiogoGoncaloProjetoFinal.DTOs
{
    // O "Carrinho" inteiro
    public class CreateOrderDto
    {
        // Uma lista de itens que o utilizador quer comprar
        [Required]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    // Cada "Linha" do carrinho
    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100)] // Impede comprar 0 ou quantidades negativas
        public int Quantity { get; set; }
    }
}