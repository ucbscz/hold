using Ardalis.Result;
using FluentAssertions;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Mantenimiento;
using IMT_Reservas.Server.Presentation.Controllers.Implementations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Controllers;

[TestFixture]
public class MantenimientoControllerTests
{
    private Mock<MantenimientoService> _serviceMock = null!;
    private MantenimientoController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<MantenimientoService>(null!, null!, null!, null!, null!, null!);
        _controller = new MantenimientoController(_serviceMock.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithData()
    {
        var data = new List<MantenimientoDto> { new MantenimientoDto { Id = 1, Costo = 100 } };
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(Result<List<MantenimientoDto>>.Success(data));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<MantenimientoDto>>;
        value.Should().NotBeNull();
        value!.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task Get_ExistingId_ReturnsOk()
    {
        var dto = new MantenimientoDto { Id = 1, Costo = 100 };
        _serviceMock.Setup(s => s.Get(1)).ReturnsAsync(Result<MantenimientoDto>.Success(dto));

        var result = await _controller.Get(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<MantenimientoDto>;
        value!.Value.Should().Be(dto);
    }

    [Test]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var dto = new MantenimientoDto { Costo = 100 };
        var created = new MantenimientoDto { Id = 1, Costo = 100 };
        _serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(Result<MantenimientoDto>.Success(created));

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var value = createdResult.Value as Response<MantenimientoDto>;
        value!.Value.Should().Be(created);
    }

    [Test]
    public async Task Update_ValidDto_ReturnsOk()
    {
        var dto = new MantenimientoDto { Costo = 200 };
        _serviceMock.Setup(s => s.Update(1, dto)).ReturnsAsync(Result<MantenimientoDto>.Success(dto));

        var result = await _controller.Update(1, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<MantenimientoDto>;
        value!.Value.Should().Be(dto);
    }

    [Test]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.Delete(1)).ReturnsAsync(Result<object>.Success(null!));

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }
}
