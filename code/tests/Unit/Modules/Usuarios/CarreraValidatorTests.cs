using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Usuarios.Application;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Tests.Unit.Modules.Usuarios;

[TestFixture]
internal class CarreraValidatorTests
{
    private ApplicationDbContext _dbContext = null!;
    private CarreraValidator _validator = null!;

    [SetUp]
    public async Task SetUp()
    {
        _dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );
        _dbContext.Carreras.Add(new Carrera { Id = 1, Nombre = "Ingeniería Mecatrónica", Codigo = "IMT" });
        await _dbContext.SaveChangesAsync();
        _validator = new CarreraValidator(_dbContext);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [TestCase(null)]
    [TestCase("imt")]
    [TestCase("I")]
    [TestCase("ING-SIS")]
    public async Task Validate_InvalidCode_Fails(string? codigo)
    {
        var result = await _validator.ValidateAsync(new CarreraDto { Nombre = "Ingeniería Industrial", Codigo = codigo });

        result.Errors.Should().Contain(error => error.PropertyName == nameof(CarreraDto.Codigo));
    }

    [Test]
    public async Task Validate_CodeOfAnotherCareer_Fails()
    {
        var result = await _validator.ValidateAsync(new CarreraDto { Nombre = "Ingeniería Industrial", Codigo = "IMT" });

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Ya existe una carrera con ese código");
    }

    [Test]
    public async Task Validate_SameCareerKeepsItsCode_Passes()
    {
        var result = await _validator.ValidateAsync(new CarreraDto { Id = 1, Nombre = "Ingeniería Mecatrónica", Codigo = "IMT" });

        result.IsValid.Should().BeTrue();
    }
}
