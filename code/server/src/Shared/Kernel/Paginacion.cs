namespace UCB_Hold.Server.Shared.Kernel;

public static class Paginacion
{
    public const int MaxPageSize = 500;

    public static (int Skip, int Take) Rango(int page, int pageSize)
    {
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var safePage = Math.Clamp(page, 1, int.MaxValue / size);
        return ((safePage - 1) * size, size);
    }
}
