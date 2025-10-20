using Microsoft.EntityFrameworkCore;
using SistemaAgendamento.Data;
using SistemaAgendamento.Models;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o contexto do banco de dados com SQLite, lendo a configuração do appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Cria o banco de dados automaticamente se não existir
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ===================================================================
// CLIENTES
// ===================================================================
app.MapGet("/clientes", async (AppDbContext db) =>
    await db.Clientes.ToListAsync());

app.MapGet("/clientes/{id}", async (AppDbContext db, int id) =>
    await db.Clientes.FindAsync(id) is Cliente cliente
        ? Results.Ok(cliente)
        : Results.NotFound());

app.MapPost("/clientes", async (AppDbContext db, Cliente cliente) =>
{
    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    return Results.Created($"/clientes/{cliente.Id}", cliente);
});

app.MapPut("/clientes/{id}", async (AppDbContext db, int id, Cliente input) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null) return Results.NotFound();

    cliente.Nome = input.Nome;
    cliente.Email = input.Email;
    cliente.Telefone = input.Telefone;
    cliente.CPF = input.CPF;

    await db.SaveChangesAsync();
    return Results.Ok(cliente);
});

app.MapDelete("/clientes/{id}", async (AppDbContext db, int id) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null) return Results.NotFound();

    db.Clientes.Remove(cliente);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===================================================================
// PROFISSIONAIS
// ===================================================================
app.MapGet("/profissionais", async (AppDbContext db) =>
    await db.Profissionais.ToListAsync());

app.MapPost("/profissionais", async (AppDbContext db, Profissional prof) =>
{
    db.Profissionais.Add(prof);
    await db.SaveChangesAsync();
    return Results.Created($"/profissionais/{prof.Id}", prof);
});

// ===================================================================
// AGENDAMENTOS
// ===================================================================
app.MapGet("/agendamentos", async (AppDbContext db) =>
    await db.Agendamentos
        .Include(a => a.Cliente)
        .Include(a => a.Profissional)
        .ToListAsync());

app.MapPost("/agendamentos", async (AppDbContext db, Agendamento agendamento) =>
{
    // Verifica se cliente e profissional existem
    var cliente = await db.Clientes.FindAsync(agendamento.ClienteId);
    var profissional = await db.Profissionais.FindAsync(agendamento.ProfissionalId);

    if (cliente is null || profissional is null)
        return Results.BadRequest("Cliente ou Profissional inválido.");

    db.Agendamentos.Add(agendamento);
    await db.SaveChangesAsync();

    return Results.Created($"/agendamentos/{agendamento.Id}", agendamento);
});

app.Run();