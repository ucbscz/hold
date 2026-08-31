using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IMT_Reservas.Server.Infrastructure.Config.EntityConfigurations;

internal sealed class AmbienteConfiguration : IEntityTypeConfiguration<Ambiente>
{
    public void Configure(EntityTypeBuilder<Ambiente> entity)
    {
        entity.ToTable("ambientes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_ambiente");
        entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
        entity.Property(e => e.EstadoEliminado).HasColumnName("estado_eliminado");
        entity.HasIndex(e => e.Nombre).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}

internal sealed class ProcedenciaConfiguration : IEntityTypeConfiguration<Procedencia>
{
    public void Configure(EntityTypeBuilder<Procedencia> entity)
    {
        entity.ToTable("procedencias");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id_procedencia");
        entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
        entity.Property(e => e.EstadoEliminado).HasColumnName("estado_eliminado");
        entity.HasIndex(e => e.Nombre).IsUnique();
        entity.HasQueryFilter(e => !e.EstadoEliminado);
    }
}
