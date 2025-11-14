using System;
using System.Collections.Generic;

namespace ApiDiogoGoncaloProjetoFinal.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
