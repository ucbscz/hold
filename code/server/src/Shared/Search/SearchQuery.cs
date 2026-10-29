namespace IMT_Reservas.Server.Application.Abstraction;

public sealed record SearchQuery(
    string? Term, IReadOnlyCollection<string> Fields, int Size = 200
);
