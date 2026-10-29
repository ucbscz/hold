namespace UCB_Hold.Server.Shared.Search;

public sealed record SearchQuery(
    string? Term, IReadOnlyCollection<string> Fields, int Size = 200
);
