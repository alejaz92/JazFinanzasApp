using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BondInterestSystemClass : Migration
    {
        // Solo datos, sin cambios de esquema: categoría de sistema "Dividendos, Intereses y Ganancias
        // Inversiones" para el interés de los cobros de bonos. Se la quita de la elección del usuario.
        // (EF también regenera aquí los timestamps de las semillas de AssetTypes; no se incluyen
        // porque no hacen nada y cada migración los pisa.)

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Quien ya la tenía creada a mano (el usuario 2): pasa a ser de sistema, sin tocar sus movimientos.
            migrationBuilder.Sql(@"
UPDATE TransactionClasses
SET IsSystem = 1, CountsAsIncomeExpense = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Description = N'Dividendos, Intereses y Ganancias Inversiones' AND IncExp = 'I';");

            // Todos los demás usuarios que todavía no la tienen.
            migrationBuilder.Sql(@"
INSERT INTO TransactionClasses (Description, IncExp, UserId, CreatedAt, UpdatedAt, IsSystem, CountsAsIncomeExpense)
SELECT N'Dividendos, Intereses y Ganancias Inversiones', 'I', u.Id, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, 1
FROM AspNetUsers u
WHERE NOT EXISTS (
    SELECT 1 FROM TransactionClasses c
    WHERE c.UserId = u.Id AND c.Description = N'Dividendos, Intereses y Ganancias Inversiones');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Se borran las que quedaron sin uso; las que ya tienen movimientos vuelven a ser comunes.
            migrationBuilder.Sql(@"
DELETE c FROM TransactionClasses c
WHERE c.Description = N'Dividendos, Intereses y Ganancias Inversiones' AND c.IsSystem = 1
  AND NOT EXISTS (SELECT 1 FROM Transactions t WHERE t.TransactionClassId = c.Id)
  AND NOT EXISTS (SELECT 1 FROM CardTransactions ct WHERE ct.TransactionClassId = c.Id);

UPDATE TransactionClasses SET IsSystem = 0
WHERE Description = N'Dividendos, Intereses y Ganancias Inversiones';");
        }
    }
}
