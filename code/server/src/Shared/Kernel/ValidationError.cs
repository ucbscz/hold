namespace IMT_Reservas.Server.Shared.Kernel;

public class ValidationError
{
    public string PropertyName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
