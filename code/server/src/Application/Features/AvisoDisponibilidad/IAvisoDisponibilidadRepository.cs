using AvisoDisponibilidadEntity = IMT_Reservas.Server.Core.Entities.AvisoDisponibilidad;

namespace IMT_Reservas.Server.Application.Features.AvisoDisponibilidad;

public interface IAvisoDisponibilidadRepository
{
    Task Add(string carnet, AvisoDisponibilidadDto dto);
    Task<List<AvisoDisponibilidadEntity>> GetPending();
    Task MarkAsNotified(IReadOnlyCollection<int> ids);
}
