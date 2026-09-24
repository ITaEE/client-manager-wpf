using Microsoft.EntityFrameworkCore;
using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Infrastructure.Data;

public sealed class ClientManagerDbContext(DbContextOptions<ClientManagerDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var client = modelBuilder.Entity<Client>();
        client.ToTable("Clients");
        client.HasKey(item => item.Id);
        client.Property(item => item.FullName).HasMaxLength(150).IsRequired();
        client.Property(item => item.Phone).HasMaxLength(50);
        client.Property(item => item.Email).HasMaxLength(254);
        client.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        client.Property(item => item.Notes).HasMaxLength(2_000);
        client.Property(item => item.CreatedAt).IsRequired();
        client.Property(item => item.UpdatedAt).IsRequired();
        client.HasIndex(item => item.FullName);
        client.HasIndex(item => item.Status);
        client.HasIndex(item => item.Phone);
        client.HasIndex(item => item.Email);
    }
}
