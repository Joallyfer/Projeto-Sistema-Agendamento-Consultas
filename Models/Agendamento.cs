namespace SistemaAgendamento.Models;

public class Agendamento
{
    public int Id { get; set; }                    // Identificador do agendamento
    public DateTime DataHora { get; set; }         // Data e hora da consulta
    public string? Observacoes { get; set; }       // Observações opcionais

    // Relacionamentos
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; }

    public int ProfissionalId { get; set; }
    public Profissional Profissional { get; set; }
}
