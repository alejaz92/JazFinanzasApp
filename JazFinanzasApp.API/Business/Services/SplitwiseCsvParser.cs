using System.Globalization;
using System.Text;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class SplitwiseMemberDelta
    {
        public string MemberName { get; set; } = string.Empty;
        public decimal Delta { get; set; }
    }

    public class SplitwiseRow
    {
        public int RowIndex { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsPayment { get; set; }
        public List<SplitwiseMemberDelta> MemberDeltas { get; set; } = new();
        public string? PayerMemberName { get; set; }
        public string? ReceiverMemberName { get; set; }

        // true cuando la fila no encaja en el modelo de "un solo pagador" que soporta SharedEventMovement/SharedEventPayment
        public bool Unsupported { get; set; }

        // monto que le corresponde consumir a cada miembro (excluye a los que no participan de esta fila)
        public Dictionary<string, decimal> ComputeShares()
        {
            var shares = new Dictionary<string, decimal>();
            foreach (var d in MemberDeltas)
            {
                if (d.Delta == 0) continue;
                var share = d.MemberName == PayerMemberName ? Cost - d.Delta : -d.Delta;
                if (share > 0.001m) shares[d.MemberName] = Math.Round(share, 2);
            }
            return shares;
        }
    }

    public class SplitwiseBalanceRow
    {
        public string Currency { get; set; } = string.Empty;
        public List<SplitwiseMemberDelta> MemberBalances { get; set; } = new();
    }

    public class SplitwiseParseResult
    {
        public List<string> MemberNames { get; set; } = new();
        public List<string> CategoryNames { get; set; } = new();
        public List<SplitwiseRow> Rows { get; set; } = new();
        public List<SplitwiseBalanceRow> BalanceRows { get; set; } = new();
    }

    // Parser del CSV exportado por Splitwise: Fecha,Descripción,Categoría,Coste,Moneda,<miembro1>,<miembro2>,...
    // Cada fila de gasto/pago trae, por columna de miembro, el delta neto que ese movimiento le produjo a su balance
    // (positivo = puso plata de más / cobró, negativo = debe). Puede haber una fila final "Saldo total" por moneda.
    public static class SplitwiseCsvParser
    {
        private const string PaymentCategory = "Pago";
        private const string BalanceRowMarker = "Saldo total";
        private const int FirstMemberColumnIndex = 5;

        public static SplitwiseParseResult Parse(string csvContent)
        {
            if (string.IsNullOrWhiteSpace(csvContent))
                throw new BusinessRuleException("El archivo está vacío");

            var lines = csvContent.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var header = SplitCsvLine(lines[0]);
            if (header.Count <= FirstMemberColumnIndex)
                throw new BusinessRuleException("El archivo no tiene el formato esperado de Splitwise (encabezado inválido)");

            var memberNames = header.Skip(FirstMemberColumnIndex).Select(m => m.Trim()).Where(m => m.Length > 0).ToList();
            if (memberNames.Count == 0)
                throw new BusinessRuleException("No se encontraron columnas de miembros en el archivo");

            var rows = new List<SplitwiseRow>();
            var balanceRows = new List<SplitwiseBalanceRow>();

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = SplitCsvLine(line);
                if (fields.Count <= FirstMemberColumnIndex - 1) continue;

                var description = fields[1].Trim();
                var currency = fields[4].Trim();

                if (description.Equals(BalanceRowMarker, StringComparison.OrdinalIgnoreCase))
                {
                    balanceRows.Add(new SplitwiseBalanceRow { Currency = currency, MemberBalances = ParseMemberDeltas(fields, memberNames) });
                    continue;
                }

                if (!DateTime.TryParse(fields[0].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    throw new BusinessRuleException($"Fila {i + 1}: fecha inválida ('{fields[0]}')");
                if (!decimal.TryParse(fields[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var cost))
                    throw new BusinessRuleException($"Fila {i + 1}: costo inválido ('{fields[3]}')");

                var category = fields[2].Trim();
                var isPayment = category.Equals(PaymentCategory, StringComparison.OrdinalIgnoreCase);
                var deltas = ParseMemberDeltas(fields, memberNames);

                var positive = deltas.Where(d => d.Delta > 0).ToList();
                var negative = deltas.Where(d => d.Delta < 0).ToList();
                var unsupported = isPayment
                    ? positive.Count != 1 || negative.Count != 1
                    : positive.Count > 1;

                rows.Add(new SplitwiseRow
                {
                    RowIndex = i,
                    Date = date,
                    Description = description,
                    Category = category,
                    Cost = cost,
                    Currency = currency,
                    IsPayment = isPayment,
                    MemberDeltas = deltas,
                    PayerMemberName = positive.Count == 1 ? positive[0].MemberName : null,
                    ReceiverMemberName = isPayment && negative.Count == 1 ? negative[0].MemberName : null,
                    Unsupported = unsupported
                });
            }

            return new SplitwiseParseResult
            {
                MemberNames = memberNames,
                CategoryNames = rows.Where(r => !r.IsPayment).Select(r => r.Category).Distinct().ToList(),
                Rows = rows.OrderBy(r => r.Date).ThenBy(r => r.RowIndex).ToList(),
                BalanceRows = balanceRows
            };
        }

        private static List<SplitwiseMemberDelta> ParseMemberDeltas(List<string> fields, List<string> memberNames)
        {
            var result = new List<SplitwiseMemberDelta>();
            for (var m = 0; m < memberNames.Count; m++)
            {
                var fieldIndex = FirstMemberColumnIndex + m;
                var raw = fieldIndex < fields.Count ? fields[fieldIndex].Trim() : "0";
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
                result.Add(new SplitwiseMemberDelta { MemberName = memberNames[m], Delta = value });
            }
            return result;
        }

        // Soporte básico de CSV (RFC4180): campos entre comillas, comillas escapadas ("")
        private static List<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else current.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                    else current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields;
        }
    }
}
