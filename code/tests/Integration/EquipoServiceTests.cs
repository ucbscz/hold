using FluentAssertions;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class EquipoServiceTests : ServiceTest<EquipoService>
{
    private const int GrupoId = 1;
    private const int GrupoId2 = 2;

    protected override EquipoService CreateService(ApplicationDbContext db)
    {
        var mapper = new EquipoMapper();
        var repo = new EquipoRepository(db, mapper);
        var validator = new EquipoValidator(db);
        var audit = new AuditLogService(new AuditLogRepository(db), new HttpContextAccessor());

        return new EquipoService(repo, mapper, validator, audit);
    }

    [SetUp]
    public async Task SeedGrupos()
    {
        Db.GruposEquipos.AddRange(
            new GrupoEquipo { Id = GrupoId, Nombre = "Grupo A", Modelo = "M1", Marca = "Marca", IdCategoria = 1 },
            new GrupoEquipo { Id = GrupoId2, Nombre = "Grupo B", Modelo = "M2", Marca = "Marca", IdCategoria = 1 }
        );
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task Create_FirstEquipo_AssignsCodigoImtOne()
    {
        var dto = BuildValidEquipo(GrupoId);

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeTrue();
        Db.Equipos.Single().CodigoImt.Should().Be(1);
    }

    [Test]
    public async Task Create_WithoutCodigoUcb_LeavesOptionalCodeEmpty()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));
        await Sut.Create(BuildValidEquipo(GrupoId));

        var codes = Db.Equipos.Select(e => e.CodigoUcb).ToList();

        codes.Should().HaveCount(2);
        codes.Should().AllSatisfy(code => code.Should().BeNull());
    }

    [Test]
    public async Task Create_WithCodigoUcb_PreservesProvidedCode()
    {
        var dto = BuildValidEquipo(GrupoId);
        dto.CodigoUcb = " UCB-INVENTARIO-01 ";

        await Sut.Create(dto);

        Db.Equipos.Single().CodigoUcb.Should().Be("UCB-INVENTARIO-01");
    }

    [Test]
    public async Task Create_SecondEquipo_AssignsNextCodigoImt()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));

        var result = await Sut.Create(BuildValidEquipo(GrupoId));

        result.IsSuccess.Should().BeTrue();
        Db.Equipos.Max(e => e.CodigoImt).Should().Be(2);
    }

    [Test]
    public async Task Create_SetsCurrentDate()
    {
        var before = DateOnly.FromDateTime(DateTime.Now);

        await Sut.Create(BuildValidEquipo(GrupoId));

        var after = DateOnly.FromDateTime(DateTime.Now);
        var fechaIngreso = Db.Equipos.Single().FechaIngresoEquipo;

        fechaIngreso.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Test]
    public async Task Create_RecalculatesGrupoStats()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));

        var grupo = Db.GruposEquipos.Single(g => g.Id == GrupoId);
        grupo.Cantidad.Should().Be(1);
    }

    [Test]
    public async Task Update_PreservesCodigoImt()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));
        var equipo = Db.Equipos.AsNoTracking().Single();

        await NewSut().Update(equipo.Id, new EquipoDto { IdGrupoEquipo = GrupoId, EstadoEquipo = "operativo", CodigoImt = 999 });

        Db.Equipos.AsNoTracking().Single(e => e.Id == equipo.Id).CodigoImt.Should().Be(equipo.CodigoImt);
    }

    [Test]
    public async Task Update_PreservesFechaIngreso()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));
        var equipo = Db.Equipos.AsNoTracking().Single();
        var originalFecha = equipo.FechaIngresoEquipo;

        await NewSut().Update(equipo.Id, BuildValidEquipo(GrupoId));

        Db.Equipos.AsNoTracking().Single(e => e.Id == equipo.Id).FechaIngresoEquipo.Should().Be(originalFecha);
    }

    [Test]
    public async Task Update_WithoutCodigoUcb_PreservesGeneratedCode()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));
        var equipo = Db.Equipos.AsNoTracking().Single();

        await NewSut().Update(equipo.Id, BuildValidEquipo(GrupoId));

        Db.Equipos.AsNoTracking().Single().CodigoUcb.Should().Be(equipo.CodigoUcb);
    }

    [Test]
    public async Task Create_WithDuplicateCodigoUcb_ReturnsError()
    {
        var first = BuildValidEquipo(GrupoId);
        first.CodigoUcb = "UCB-000001";
        var duplicate = BuildValidEquipo(GrupoId2);
        duplicate.CodigoUcb = "UCB-000001";
        await Sut.Create(first);

        var result = await Sut.Create(duplicate);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Código UCB ya registrado");
        Db.Equipos.Should().ContainSingle();
    }

    [Test]
    public async Task Update_GroupChanged_RecalculatesBothGroups()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));
        var equipo = Db.Equipos.AsNoTracking().Single();

        await NewSut().Update(equipo.Id, BuildValidEquipo(GrupoId2));

        Db.GruposEquipos.AsNoTracking().Single(g => g.Id == GrupoId).Cantidad.Should().Be(0);
        Db.GruposEquipos.AsNoTracking().Single(g => g.Id == GrupoId2).Cantidad.Should().Be(1);
    }

    [Test]
    public async Task Delete_ExistingEquipo_RecalculatesGrupoStats()
    {
        await Sut.Create(BuildValidEquipo(GrupoId));
        var equipo = Db.Equipos.Single();

        await Sut.Delete(equipo.Id);

        Db.GruposEquipos.Single(g => g.Id == GrupoId).Cantidad.Should().Be(0);
    }

    [Test]
    public async Task Delete_NonExistentEquipo_ReturnsNotFound()
    {
        var result = await Sut.Delete(99);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Test]
    public async Task Create_InvalidGrupo_ReturnsValidationError()
    {
        var dto = BuildValidEquipo(idGrupo: 999);

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors
            .Select(error => error.ErrorMessage)
            .Should()
            .Contain("Grupo equipo no existe");
    }

    private static EquipoDto BuildValidEquipo(int idGrupo) => new()
    {
        IdGrupoEquipo = idGrupo,
        EstadoEquipo = "operativo"
    };
}
