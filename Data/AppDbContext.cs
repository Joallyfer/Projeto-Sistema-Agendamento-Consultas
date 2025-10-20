using Microsoft.EntityFrameworkCore;
using SistemaAgendamento.Models;

namespace SistemaAgendamento.Data;

// Classe responsável por conectar o banco de dados com o Código
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Profissional> Profissionais { get; set; }
    public DbSet<Agendamento> Agendamentos { get; set; }
}
