using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Prestamo.State;
using IMT_Reservas.Server.Core.Entities;
namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class PrestamoStateTests
{
    [TestCase(EstadoPrestamo.Pendiente, EstadoPrestamo.Aprobado, true)]
    [TestCase(EstadoPrestamo.Pendiente, EstadoPrestamo.Rechazado, true)]
    [TestCase(EstadoPrestamo.Pendiente, EstadoPrestamo.Cancelado, true)]
    [TestCase(EstadoPrestamo.Aprobado, EstadoPrestamo.Activo, true)]
    [TestCase(EstadoPrestamo.Aprobado, EstadoPrestamo.Cancelado, true)]
    [TestCase(EstadoPrestamo.Activo, EstadoPrestamo.Finalizado, true)]
    [TestCase(EstadoPrestamo.Activo, EstadoPrestamo.Cancelado, true)]
    [TestCase(EstadoPrestamo.Pendiente, EstadoPrestamo.Activo, false)]
    [TestCase(EstadoPrestamo.Aprobado, EstadoPrestamo.Pendiente, false)]
    [TestCase(EstadoPrestamo.Aprobado, EstadoPrestamo.Rechazado, false)]
    [TestCase(EstadoPrestamo.Activo, EstadoPrestamo.Pendiente, false)]
    [TestCase(EstadoPrestamo.Activo, EstadoPrestamo.Aprobado, false)]
    public void CanTransition_ReturnsExpected(EstadoPrestamo from, EstadoPrestamo to, bool expected)
    {
        var result = PrestamoState.CanTransition(from, to);

        result.Should().Be(expected);
    }

    [Test]
    public void CanTransition_FromTerminalState_AlwaysFalse(
        [Values(EstadoPrestamo.Finalizado, EstadoPrestamo.Rechazado, EstadoPrestamo.Cancelado)] EstadoPrestamo terminal,
        [Values] EstadoPrestamo target)
    {
        var result = PrestamoState.CanTransition(terminal, target);

        result.Should().BeFalse();
    }

    [TestCase("pendiente", EstadoPrestamo.Pendiente)]
    [TestCase("aprobado", EstadoPrestamo.Aprobado)]
    [TestCase("activo", EstadoPrestamo.Activo)]
    [TestCase("rechazado", EstadoPrestamo.Rechazado)]
    [TestCase("finalizado", EstadoPrestamo.Finalizado)]
    [TestCase("cancelado", EstadoPrestamo.Cancelado)]
    [TestCase("PENDIENTE", EstadoPrestamo.Pendiente)]
    [TestCase("Aprobado", EstadoPrestamo.Aprobado)]
    public void Parse_ValidString_ReturnsEnum(string text, EstadoPrestamo expected)
    {
        var result = PrestamoState.Parse(text);

        result.Should().Be(expected);
    }

    [TestCase("desconocido")]
    [TestCase("")]
    [TestCase("en_proceso")]
    public void Parse_InvalidString_ReturnsNull(string text)
    {
        var result = PrestamoState.Parse(text);

        result.Should().BeNull();
    }

    [Test]
    public void Parse_NullInput_ReturnsNull()
    {
        var result = PrestamoState.Parse(null);

        result.Should().BeNull();
    }

    [Test]
    public void ToText_AllStates_RoundTripWithParse()
    {
        foreach (var estado in Enum.GetValues<EstadoPrestamo>())
        {
            var text = PrestamoState.ToText(estado);
            var parsed = PrestamoState.Parse(text);

            parsed.Should().Be(estado, $"ToText then Parse should round-trip for {estado}");
        }
    }
}
