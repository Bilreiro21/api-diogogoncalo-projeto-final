using System;
using System.Collections.Generic;
using ApiDiogoGoncaloProjetoFinal.Models;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ApiDiogoGoncaloProjetoFinal.Data;

// Esta classe herda de DbContext, o que a torna capaz de falar com a Base de Dados,
// é aqui que definimos quais as tabelas que existem e como elas se comportam.
public partial class ApplicationDbContext : DbContext
{
    // Construtor vazio
    public ApplicationDbContext()
    {
    }

    // aqui temos o contrutor principal
    // Recebe as "options" (configurações como a string de conexão) que definimos no Program.cs
    // e passa-as para a classe base (DbContext).
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Representação das tabelas da base de dados
    // Cada DbSet representa uma tabela inteira na base de dados.

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<User> Users { get; set; }

    // Este método configura a ligação à BD caso ela não tenha sido configurada no Program.cs.
    // O código gerado automaticamente (Scaffold) mete aqui a string de conexão.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=projetofinal_db;user=root;password=123456789", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.1.0-mysql"));

    // Este método corre quando o EF Core está a criar o modelo na memória.
    // aqui definimos regras que não conseguimos definir apenas com atributos nas classes Models.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // conjunto de caracteres UTF8 
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        // configuração da tabela ORDERS
        modelBuilder.Entity<Order>(entity =>
        {
            // Chave Primária (PK)
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            // cria um índice para a coluna UserId (para as pesquisas por utilizador serem rápidas)
            entity.HasIndex(e => e.UserId, "FK_Orders_Users_idx");

            // define que a data é Datetime e que, se não enviarmos nada, usa a data atual do MySQL
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            // define o tamanho máximo do Status e mete 'Pending' como valor por defeito
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'");

            entity.Property(e => e.TransactionId).HasMaxLength(255);

            // Define a Relação: uma rrder tem um user. um user tem várias orders.
            // chave estrangeira (FK)
            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull) // se apagarmos o user, não apaga a order em cascata
                .HasConstraintName("FK_Orders_Users");
        });

        // Configuração da Tabela ORDER DETAILS
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            // Índices para performance nas chaves estrangeiras
            entity.HasIndex(e => e.OrderId, "FK_OrderDetails_Orders_idx");
            entity.HasIndex(e => e.ProductId, "FK_OrderDetails_Products_idx");

            // é importante para dinheiro, garante 2 casas decimais exatas
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);

            // Relação: Este detalhe pertence a uma order
            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Orders");

            // Relação: Este detalhe refere-se a um product
            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Products");
        });

        // Configuração da Tabela PRODUCTS
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            // Cria um índice ÚNICO no SKU.
            // isto vai impedir com que existam dois produtos com o mesmo código SKU.
            entity.HasIndex(e => e.Sku, "IX_Products_SKU").IsUnique();

            entity.Property(e => e.Description).HasColumnType("text"); // permite textos longos
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .HasColumnName("SKU");
        });

        // Configuração da Tabela USERS
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            // Cria um índice ÚNICO no Email.
            // garante que não conseguimos criar duas contas com o mesmo email.
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}