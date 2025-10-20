using Microsoft.EntityFrameworkCore;
using SistemaAgendamento.Data;
using SistemaAgendamento.Models;
using System.Text.Json.Serialization; // 👈 necessário para o ReferenceHandler

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// CONFIGURAÇÃO DO BANCO DE DADOS
// ================================================================

// Responsável por configurar o serviço de banco de dados utilizando Entity Framework 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ================================================================
// CONFIGURAÇÃO DO JSON (evita erro de ciclo infinito no relacionamento)
// ================================================================
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // 👈 evita loop Cliente↔Agendamento
    options.SerializerOptions.WriteIndented = true; // 👈 deixa o JSON formatado e legível
});

var app = builder.Build();

// ================================================================
// CRIA O BANCO E AS TABELAS AUTOMATICAMENTE
// ================================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ================================================================
// CLIENTES - CRUD
// ================================================================

// Listar todos
app.MapGet("/clientes", async (AppDbContext db) =>
    await db.Clientes.ToListAsync());

// Buscar por ID
app.MapGet("/clientes/{id:int}", async (AppDbContext db, int id) =>
    await db.Clientes.FindAsync(id) is Cliente c
        ? Results.Ok(c)
        : Results.NotFound("Cliente não encontrado."));

// Criar
app.MapPost("/clientes", async (AppDbContext db, Cliente input) =>
{
    db.Clientes.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/clientes/{input.Id}", input);
});

// Atualizar
app.MapPut("/clientes/{id:int}", async (AppDbContext db, int id, Cliente input) =>
{
    var c = await db.Clientes.FindAsync(id);
    if (c is null) return Results.NotFound("Cliente não encontrado.");

    c.Nome = input.Nome;
    c.Email = input.Email;
    c.Telefone = input.Telefone;
    c.CPF = input.CPF;

    await db.SaveChangesAsync();
    return Results.Ok(c);
});

// Remover
app.MapDelete("/clientes/{id:int}", async (AppDbContext db, int id) =>
{
    var c = await db.Clientes.FindAsync(id);
    if (c is null) return Results.NotFound("Cliente não encontrado.");

    db.Clientes.Remove(c);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ================================================================
// PROFISSIONAIS - CRUD
// ================================================================

// Listar todos
app.MapGet("/profissionais", async (AppDbContext db) =>
    await db.Profissionais.ToListAsync());

// Buscar por ID
app.MapGet("/profissionais/{id:int}", async (AppDbContext db, int id) =>
    await db.Profissionais.FindAsync(id) is Profissional p
        ? Results.Ok(p)
        : Results.NotFound("Profissional não encontrado."));

// Criar
app.MapPost("/profissionais", async (AppDbContext db, Profissional input) =>
{
    db.Profissionais.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/profissionais/{input.Id}", input);
});

// Atualizar
app.MapPut("/profissionais/{id:int}", async (AppDbContext db, int id, Profissional input) =>
{
    var p = await db.Profissionais.FindAsync(id);
    if (p is null) return Results.NotFound("Profissional não encontrado.");

    p.Nome = input.Nome;
    p.Especialidade = input.Especialidade;

    await db.SaveChangesAsync();
    return Results.Ok(p);
});

// Remover
app.MapDelete("/profissionais/{id:int}", async (AppDbContext db, int id) =>
{
    var p = await db.Profissionais.FindAsync(id);
    if (p is null) return Results.NotFound("Profissional não encontrado.");

    db.Profissionais.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ================================================================
// AGENDAMENTOS - CRUD (com includes e regra de conflito de horário)
// ================================================================

// Listar todos (com Cliente e Profissional)
app.MapGet("/agendamentos", async (AppDbContext db) =>
    await db.Agendamentos
        .Include(a => a.Cliente)
        .Include(a => a.Profissional)
        .ToListAsync());

// Buscar por ID (com Cliente e Profissional)
app.MapGet("/agendamentos/{id:int}", async (AppDbContext db, int id) =>
    await db.Agendamentos
        .Include(a => a.Cliente)
        .Include(a => a.Profissional)
        .FirstOrDefaultAsync(a => a.Id == id)
        is Agendamento ag
            ? Results.Ok(ag)
            : Results.NotFound("Agendamento não encontrado."));

// Criar (valida existência e conflito de horário do profissional)
app.MapPost("/agendamentos", async (AppDbContext db, Agendamento input) =>
{
    var cliente = await db.Clientes.FindAsync(input.ClienteId);
    var profissional = await db.Profissionais.FindAsync(input.ProfissionalId);
    if (cliente is null || profissional is null)
        return Results.BadRequest("Cliente ou Profissional inválido.");

    bool conflito = await db.Agendamentos.AnyAsync(a =>
        a.ProfissionalId == input.ProfissionalId &&
        a.DataHora == input.DataHora);

    if (conflito)
        return Results.BadRequest("Já existe um agendamento nesse horário para este profissional.");

    db.Agendamentos.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/agendamentos/{input.Id}", input);
});

// Atualizar (revalida conflito quando muda data/profissional)
app.MapPut("/agendamentos/{id:int}", async (AppDbContext db, int id, Agendamento input) =>
{
    var ag = await db.Agendamentos.FindAsync(id);
    if (ag is null) return Results.NotFound("Agendamento não encontrado.");

    if (await db.Clientes.FindAsync(input.ClienteId) is null ||
        await db.Profissionais.FindAsync(input.ProfissionalId) is null)
        return Results.BadRequest("Cliente ou Profissional inválido.");

    bool conflito = await db.Agendamentos.AnyAsync(a =>
        a.Id != id &&
        a.ProfissionalId == input.ProfissionalId &&
        a.DataHora == input.DataHora);

    if (conflito)
        return Results.BadRequest("Conflito de horário: este profissional já possui agendamento neste horário.");

    ag.DataHora = input.DataHora;
    ag.Observacoes = input.Observacoes;
    ag.ClienteId = input.ClienteId;
    ag.ProfissionalId = input.ProfissionalId;

    await db.SaveChangesAsync();
    return Results.Ok(ag);
});

// Remover
app.MapDelete("/agendamentos/{id:int}", async (AppDbContext db, int id) =>
{
    var ag = await db.Agendamentos.FindAsync(id);
    if (ag is null) return Results.NotFound("Agendamento não encontrado.");

    db.Agendamentos.Remove(ag);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
