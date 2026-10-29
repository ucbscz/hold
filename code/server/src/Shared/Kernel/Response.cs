namespace IMT_Reservas.Server.Application.Abstraction;

public class Response<T>
    where T : class
{
    public int Status { get; init; }
    public T? Value { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<ValidationError> ValidationErrors { get; init; } = [];
}
