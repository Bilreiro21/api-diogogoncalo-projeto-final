using System.Text.Json.Serialization;

namespace ApiDiogoGoncaloProjetoFinal.DTOs
{
    public class SupplierStockDto
    {
        // O nome da propriedade JSON é "sku", mapeamos para Sku
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }

        [JsonPropertyName("expectedShipping")]
        public string ExpectedShipping { get; set; } = string.Empty;

        [JsonPropertyName("supplier")]
        public string SupplierName { get; set; } = string.Empty;
    }
}