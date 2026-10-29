namespace IMT_Reservas.Server.Shared.Kernel;

public static class MapperRegistration
{
    public static IServiceCollection AddMapper<TEntity, TDto, TMapper>(this IServiceCollection services)
        where TMapper : class, IMapper<TEntity, TDto>
    {
        services.AddSingleton<TMapper>();
        return services.AddSingleton<IMapper<TEntity, TDto>>(sp => sp.GetRequiredService<TMapper>());
    }
}
