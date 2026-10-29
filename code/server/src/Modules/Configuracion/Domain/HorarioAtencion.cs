namespace IMT_Reservas.Server.Core.Entities;

public sealed record HorarioAtencion
{
    public int DiaSemana { get; set; }
    public DateOnly? Fecha { get; set; }
    public bool Abierto { get; set; } = true;
    public int InicioMinutos { get; set; } = 480;
    public int FinMinutos { get; set; } = 1080;
}
