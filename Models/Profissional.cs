namespace SistemaAgendamento.Models;

public class Profissional
{
    public int Id { get; set; }                 // Identificador único do profissional
    public string Nome { get; set; }            // Nome completo
    public string Especialidade { get; set; }   // Ex: Ortodontia, Endodontia, etc.
    public string CRO { get; set; }             // Registro profissional

    // Um profissional pode ter vários agendamentos
    public List<Agendamento> Agendamentos { get; set; } = new();
}