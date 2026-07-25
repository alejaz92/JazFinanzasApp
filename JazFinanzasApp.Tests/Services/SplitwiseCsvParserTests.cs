using FluentAssertions;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;

namespace JazFinanzasApp.Tests.Services
{
    public class SplitwiseCsvParserTests
    {
        // Recorte real de una exportación de Splitwise (grupo de viaje con 4 miembros, ARS y USD,
        // filas de gasto con un solo pagador, filas "Pago" (settle up) y la fila final "Saldo total" por moneda).
        private const string SampleCsv = """
            Fecha,Descripción,Categoría,Coste,Moneda,Violeta Buzzini,Alejandro Jazmatie,Redo Donnet,Genaro Carnino

            2026-01-17,Vuelos cuota 1,Avión,175660.95,ARS,-58553.65,117107.30,-58553.65,0.00
            2026-02-03,Violeta B. pagó Alejandro J.,Pago,116.14,USD,116.14,-116.14,0.00,0.00
            2026-03-07,Van Travel,Transporte - Otro,330000.00,ARS,-110000.00,-110000.00,220000.00,0.00
            2026-03-20,Foto aerosilla,General,8000.00,ARS,0.00,4000.00,0.00,-4000.00

            2026-07-10,Saldo total, , ,ARS,0.00,0.00,0.00,0.00
            2026-07-10,Saldo total, , ,USD,0.00,0.00,0.00,0.00
            """;

        [Fact]
        public void Parse_ReadsMemberNamesFromHeader()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);

            result.MemberNames.Should().Equal("Violeta Buzzini", "Alejandro Jazmatie", "Redo Donnet", "Genaro Carnino");
        }

        [Fact]
        public void Parse_ExcludesBalanceRowsFromRows_ButCapturesThemSeparately()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);

            result.Rows.Should().HaveCount(4);
            result.BalanceRows.Should().HaveCount(2);
            result.BalanceRows.Should().OnlyContain(b => b.MemberBalances.All(m => m.Delta == 0));
        }

        [Fact]
        public void Parse_SingleExpenseWithOnePayer_ComputesSharesForEveryone()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);
            var flight = result.Rows.Single(r => r.Description == "Vuelos cuota 1");

            flight.IsPayment.Should().BeFalse();
            flight.Unsupported.Should().BeFalse();
            flight.PayerMemberName.Should().Be("Alejandro Jazmatie");

            var shares = flight.ComputeShares();
            shares.Should().HaveCount(3);
            shares["Alejandro Jazmatie"].Should().Be(58553.65m); // 175660.95 - 117107.30 (pagó y también consumió su parte)
            shares["Violeta Buzzini"].Should().Be(58553.65m);
            shares["Redo Donnet"].Should().Be(58553.65m);
            shares.Should().NotContainKey("Genaro Carnino"); // delta 0 => no participó
        }

        [Fact]
        public void Parse_PaymentRow_IdentifiesPayerAndReceiverFromSignOfDelta()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);
            var payment = result.Rows.Single(r => r.IsPayment);

            // "Violeta B. pagó Alejandro J." => Violeta puso la plata (delta +), Alejandro la recibió (delta -)
            payment.PayerMemberName.Should().Be("Violeta Buzzini");
            payment.ReceiverMemberName.Should().Be("Alejandro Jazmatie");
            payment.Cost.Should().Be(116.14m);
            payment.Currency.Should().Be("USD");
        }

        [Fact]
        public void Parse_TwoPersonSplitWhereOthersDontParticipate_OnlyIncludesInvolvedMembers()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);
            var row = result.Rows.Single(r => r.Description == "Foto aerosilla");

            row.PayerMemberName.Should().Be("Alejandro Jazmatie");
            var shares = row.ComputeShares();
            shares.Should().HaveCount(2);
            shares["Alejandro Jazmatie"].Should().Be(4000m);
            shares["Genaro Carnino"].Should().Be(4000m);
        }

        [Fact]
        public void Parse_CategoryNames_ExcludesPaymentRows()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);

            result.CategoryNames.Should().Contain(new[] { "Avión", "Transporte - Otro", "General" });
            result.CategoryNames.Should().NotContain("Pago");
        }

        [Fact]
        public void Parse_RowsAreOrderedChronologically()
        {
            var result = SplitwiseCsvParser.Parse(SampleCsv);

            result.Rows.Select(r => r.Date).Should().BeInAscendingOrder();
        }

        [Fact]
        public void Parse_MultiplePayersOnAnExpenseRow_IsFlaggedUnsupported()
        {
            var csv = """
                Fecha,Descripción,Categoría,Coste,Moneda,A,B
                2026-01-01,Gasto raro,General,100.00,ARS,50.00,50.00
                """;

            var result = SplitwiseCsvParser.Parse(csv);

            result.Rows.Single().Unsupported.Should().BeTrue();
        }

        [Fact]
        public void Parse_PaymentRowWithoutExactlyOnePayerAndReceiver_IsFlaggedUnsupported()
        {
            var csv = """
                Fecha,Descripción,Categoría,Coste,Moneda,A,B,C
                2026-01-01,Pago raro,Pago,100.00,ARS,50.00,50.00,-100.00
                """;

            var result = SplitwiseCsvParser.Parse(csv);

            result.Rows.Single().Unsupported.Should().BeTrue();
        }

        [Fact]
        public void Parse_EmptyContent_ThrowsBusinessRuleException()
        {
            Action act = () => SplitwiseCsvParser.Parse("");

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Parse_HeaderWithoutMemberColumns_ThrowsBusinessRuleException()
        {
            Action act = () => SplitwiseCsvParser.Parse("Fecha,Descripción,Categoría,Coste,Moneda");

            act.Should().Throw<BusinessRuleException>();
        }
    }
}
