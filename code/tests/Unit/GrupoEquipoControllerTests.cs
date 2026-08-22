using Ardalis.Result;
using FluentAssertions;
using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using IMT_Reservas.Server.Presentation.Controllers.Implementations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Controllers;

[TestFixture]
public class GrupoEquipoControllerTests
{
    private Mock<GrupoEquipoService> _serviceMock = null!;
    private Mock<ComentarioEquipoService> _comentariosMock = null!;
    private GrupoEquipoController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<GrupoEquipoService>(null!, null!, null!, null!, null!, null!);
        _comentariosMock = new Mock<ComentarioEquipoService>(null!, null!);
        _controller = new GrupoEquipoController(_serviceMock.Object, _comentariosMock.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithData()
    {
        var data = new List<GrupoEquipoDto> { new GrupoEquipoDto { Id = 1, Nombre = "G1" } };
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(Result<List<GrupoEquipoDto>>.Success(data));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<GrupoEquipoDto>>;
        value.Should().NotBeNull();
        value!.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task Get_ExistingId_ReturnsOk()
    {
        var dto = new GrupoEquipoDto { Id = 1, Nombre = "G1" };
        _serviceMock.Setup(s => s.Get(1)).ReturnsAsync(Result<GrupoEquipoDto>.Success(dto));

        var result = await _controller.Get(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<GrupoEquipoDto>;
        value!.Value.Should().Be(dto);
    }

    [Test]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var dto = new GrupoEquipoDto { Nombre = "G1" };
        var created = new GrupoEquipoDto { Id = 1, Nombre = "G1" };
        _serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(Result<GrupoEquipoDto>.Success(created));

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var value = createdResult.Value as Response<GrupoEquipoDto>;
        value!.Value.Should().Be(created);
    }

    [Test]
    public async Task Update_ValidDto_ReturnsOk()
    {
        var dto = new GrupoEquipoDto { Nombre = "G1-Updated" };
        _serviceMock.Setup(s => s.Update(1, dto)).ReturnsAsync(Result<GrupoEquipoDto>.Success(dto));

        var result = await _controller.Update(1, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<GrupoEquipoDto>;
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
    public async Task GetAll_WithSearchParams_ReturnsOk()
    {
        var data = new List<GrupoEquipoDto> { new GrupoEquipoDto { Id = 1, Nombre = "Osciloscopio" } };
        _serviceMock.Setup(s => s.Search("Osci", null)).ReturnsAsync(Result<List<GrupoEquipoDto>>.Success(data));

        var result = await _controller.GetAll("Osci", null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as Response<List<GrupoEquipoDto>>;
        value!.Value.Should().HaveCount(1);
    }
}
