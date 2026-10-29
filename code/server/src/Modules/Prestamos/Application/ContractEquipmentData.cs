namespace UCB_Hold.Server.Modules.Prestamos.Application;

public sealed record ContractEquipmentData(
    int IdGrupoEquipo,
    int CodigoImt,
    string? CodigoUcb,
    string? NumeroSerial
);
