namespace IMT_Reservas.Server.Modules.Prestamos.Application;

public sealed record ContractEquipmentData(
    int IdGrupoEquipo,
    int CodigoImt,
    string? CodigoUcb,
    string? NumeroSerial
);
