namespace JazFinanzasApp.API.Business.DTO.Dashboard
{
    // Fase 16 (Bloque D): lo que Inicio y el Panorama de Reportes muestran, calculado una sola vez
    // (objetivo del checkpoint: "cada indicador coincide con el reporte que lo desarrolla" — todos
    // los números salen de los mismos servicios que ya usan esos reportes, nunca recalculados acá).
    public class DashboardDTO
    {
        public DashboardIndicatorsDTO Indicators { get; set; } = new();
        public DashboardThermometerDTO Thermometer { get; set; } = new();
        public List<DashboardPendingItemDTO> Pending { get; set; } = new();
    }
}
