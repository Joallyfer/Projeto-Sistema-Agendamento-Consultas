namespace SistemaAgendamento.Models;

public class Cliente
{
    public int Id { get; set; }                // Identificador único do cliente
    public string Nome { get; set; }           // Nome completo do cliente
    public string Email { get; set; }          // E-mail para contato
    public string Telefone { get; set; }       // Telefone do cliente
    public string CPF { get; set; }            // CPF (identificação única)

    // Um cliente pode ter vários agendamentos
    public List<Agendamento> Agendamentos { get; set; } = new();
}
