namespace JazFinanzasApp.API.Business.DTO.Dashboard
{
    // Sección 5.3: una fila por pendiente, ordenada por urgencia (mismo orden en que las arma
    // DashboardService.BuildPendingAsync: tarjetas, eventos, viajes). `Kind` es el contrato con el
    // frontend para elegir el ícono y la acción ("Pagar" / "Ver evento" / "Cargar gasto") — CardDue /
    // OpenSharedEvent / TripWithoutRecentExpense.
    //
    // Corrección 2026-09-08: se sacaron los reintegros pendientes de acá. El usuario, viendo la
    // bandeja con su propia cuenta, señaló que un reintegro ya acreditado (PendingToApply > 0, lo
    // único que este ítem mostraba) no es algo que requiera su atención — es plata que ya es suya, lo
    // único pendiente es que el sistema la empareje contra una cuota futura, y eso pasa solo sin que
    // el usuario tenga que hacer nada. Lo que sí ameritaría un aviso (un reintegro PROMETIDO pero
    // todavía no acreditado, PendingToCredit > 0) no se agregó: el usuario pidió acotar la bandeja a
    // tarjetas por vencer y deudas/saldos que lo involucran directamente, sin mencionar reintegros en
    // ninguna de las dos formas.
    public class DashboardPendingItemDTO
    {
        public string Kind { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public decimal? Amount { get; set; }
        public string? AssetSymbol { get; set; }
        public DateTime? Date { get; set; }

        // "info" (default) / "warning" / "danger" — para que el frontend distinga visualmente una
        // tarjeta vencida de una que solo vence pronto (la pantalla vieja de Inicio ya lo hacía con
        // alert-warning/alert-danger). Solo CardDue tiene dos niveles reales; el resto entra o no
        // entra a la bandeja, no hay grados de urgencia que mostrar.
        public string Severity { get; set; } = "info";

        // Id de la tarjeta / evento / viaje, según Kind — para armar el link "Ver X" sin otra consulta.
        public int? LinkId { get; set; }
    }
}
