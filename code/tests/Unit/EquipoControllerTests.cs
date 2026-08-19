using Ardalis.Result;
using FluentAssertions;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Presentation.Controllers.Implementations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Controllers;

[TestFixture]
public class EquipoControllerTests
{
    private Mock<EquipoService> _serviceMock = null!;
    private EquipoController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<EquipoService>(null!, null!, null!, null!);
        _controller = new EquipoController(_serviceMock.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithData()
    {
        var data = new List<EquipoDto> { new EquipoDto { Id = 1, NumeroSerial = "S1" } };
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(Result<List<EquipoDto>>.Success(data));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<EquipoDto>>;
        value.Should().NotBeNull();
        value!.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task Get_ExistingId_ReturnsOk()
    {
        var dto = new EquipoDto { Id = 1, NumeroSerial = "S1" };
        _serviceMock.Setup(s => s.Get(1)).ReturnsAsync(Result<EquipoDto>.Success(dto));

        var result = await _controller.Get(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<EquipoDto>;
        value!.Value.Should().Be(dto);
    }

    [Test]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var dto = new EquipoDto { NumeroSerial = "S1" };
        var created = new EquipoDto { Id = 1, NumeroSerial = "S1" };
        _serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(Result<EquipoDto>.Success(created));

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var value = createdResult.Value as Response<EquipoDto>;
        value!.Value.Should().Be(created);
    }

    [Test]
    public async Task Update_ValidDto_ReturnsOk()
    {
        var dto = new EquipoDto { NumeroSerial = "S1-Updated" };
        _serviceMock.Setup(s => s.Update(1, dto)).ReturnsAsync(Result<EquipoDto>.Success(dto));

        var result = await _controller.Update(1, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<EquipoDto>;
        value!.Value.Should().Be(dto);
    }

    [Test]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.Delete(1)).ReturnsAsync(Result<object>.Success(null!));

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task GetAll_WithGrupoFilter_ReturnsOk()
    {
        var data = new List<EquipoDto> { new EquipoDto { Id = 1 } };
        _serviceMock.Setup(s => s.GetByGrupo(2)).ReturnsAsync(Result<List<EquipoDto>>.Success(data));

        var result = await _controller.GetAll(grupoId: 2);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<EquipoDto>>;
        value!.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task GetAll_WithGaveteroFilter_ReturnsOk()
    {
        var data = new List<EquipoDto> { new EquipoDto { Id = 1 } };
        _serviceMock.Setup(s => s.GetByGavetero(3)).ReturnsAsync(Result<List<EquipoDto>>.Success(data));

        var result = await _controller.GetAll(gaveteroId: 3);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<EquipoDto>>;
        value!.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task GetPrestamos_ReturnsOk()
    {
        var data = new List<HistorialEquipoDto> { new HistorialEquipoDto { IdPrestamo = 1 } };
        _serviceMock.Setup(s => s.GetHistorial(1)).ReturnsAsync(Result<List<HistorialEquipoDto>>.Success(data));

        var result = await _controller.GetPrestamos(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<HistorialEquipoDto>>;
        value!.Value.Should().HaveCount(1);
    }
}
