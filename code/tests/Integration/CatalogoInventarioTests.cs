using Ardalis.Result;
using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Inventario;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;

namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class CatalogoInventarioTests : ServiceTest<CatalogoInventarioRepository<Ambiente>>
{
    protected override CatalogoInventarioRepository<Ambiente> CreateService(ApplicationDbContext db) => new(db, new AmbienteMapper());

    [Test]
    public async Task Create_TrimsAndRejectsDuplicateName()
    {
        (await Sut.Create(new Ambiente { Nombre = " Laboratorio " })).IsSuccess.Should().BeTrue();
        (await Sut.Create(new Ambiente { Nombre = "Laboratorio" })).Status.Should().Be(ResultStatus.Conflict);
    }

    [Test]
    public async Task Delete_RejectsCatalogAssignedToEquipment()
    {
        var result = await Sut.Create(new Ambiente { Nombre = "Sala principal" });
        Db.Equipos.Add(new Equipo { Id = 1, IdAmbiente = result.Value.Id, CodigoImt = 1 });
        await Db.SaveChangesAsync();
        (await Sut.Delete(result.Value.Id!.Value)).Status.Should().Be(ResultStatus.Conflict);
    }
}
