namespace JazFinanzasApp.API.Business.Services
{
    // Qué movimientos pueden llegar a tener comercio (Fase 10, plan-rediseno-reportes.md).
    //
    // El resolver original tomaba cualquier fila con MerchantId nulo, sin mirar el tipo de
    // movimiento: sobre el historial real eso creaba "comercios" como "intercambio" (236
    // transferencias entre cuentas propias), "refund" o "evento compartido". Un comercio es
    // siempre la contraparte de un **gasto**, así que:
    //
    //   - Solo egresos (MovementType "E") y consumos de tarjeta. Los ingresos y las
    //     transferencias internas ("EX") nunca tienen comercio.
    //   - Nunca las cuotas de tarjeta (Transaction.CardTransactionId != null): el gasto vive en
    //     el CardTransaction y la cuota es flujo de caja (convención del CLAUDE.md del backend).
    //     Sin esto, una compra en 6 cuotas contaba 7 veces para el mismo comercio, porque el
    //     normalizador borra "tarjeta" y los números y "(Tarjeta | 3/6) Compra traje" cae en el
    //     mismo grupo que su consumo.
    //   - Nunca los detalles que escribe la propia app en vez del usuario.
    //
    // El filtro por tipo de movimiento vive en la query del repositorio; acá queda la parte que
    // depende del texto, que es pura y se puede testear aislada.
    public static class MerchantEligibility
    {
        // Detalles generados por la app (ajustes de saldo, cambio de moneda, liquidaciones de
        // eventos compartidos, recuentos de efectivo). Se comparan ya normalizados.
        private static readonly HashSet<string> SystemDetails = new(StringComparer.Ordinal)
        {
            "general",
            "balanceadj",
            "ajuste",
            "recuento",
            "intercambio",
            "refund",
            "deposito",
            "currency exchange",
            "trading",
            "reintegro otra cuenta",
            "evento compartido"
        };

        public static bool IsSystemDetail(string? detail)
            => SystemDetails.Contains(MerchantTextNormalizer.Normalize(detail));
    }
}
