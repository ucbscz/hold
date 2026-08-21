namespace IMT_Reservas.Server.Application.Features.Contrato;

public sealed record ContractEquipmentData(
    int IdGrupoEquipo,
    int CodigoImt,
    string? CodigoUcb,
    string? NumeroSerial
);
