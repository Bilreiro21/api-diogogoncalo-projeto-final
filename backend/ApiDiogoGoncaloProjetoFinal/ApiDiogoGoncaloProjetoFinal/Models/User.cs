using System;
using System.Collections.Generic;

namespace ApiDiogoGoncaloProjetoFinal.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Name { get; set; }

    public DateTime RegistrationDate { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
