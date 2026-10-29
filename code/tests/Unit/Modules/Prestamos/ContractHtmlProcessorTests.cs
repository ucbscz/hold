using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Contrato;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class ContractHtmlProcessorTests
{
    [Test]
    public void RenderInstitutionalSigner_ReplacesCurrentAndLegacySignerData()
    {
        const string signature = "data:image/png;base64,aGVsbG8=";
        var html = """
            <div>
              <strong data-contract-field="institutional-name">Job Angel Ledezma Pérez</strong>
              <strong data-contract-field="institutional-carnet">5268336 CB</strong>
              <div class="signature">
                <div>
                  <img data-contract-image="institutional-signature" src="data:image/png;base64,b2xk" />
                  <p>Job Angel Ledezma Pérez</p>
                </div>
              </div>
            </div>
            """;

        var result = new ContractHtmlProcessor().RenderInstitutionalSigner(
            html,
            "Ana Pérez",
            "12890061",
            signature
        );

        result.Should().Contain("Ana Pérez");
        result.Should().Contain("12890061");
        result.Should().Contain(signature);
        result.Should().NotContain("Job Angel Ledezma Pérez");
        result.Should().NotContain("5268336 CB");
    }
}
