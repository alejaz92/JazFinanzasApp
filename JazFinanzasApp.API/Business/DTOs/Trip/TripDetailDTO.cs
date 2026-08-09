namespace JazFinanzasApp.API.Business.DTO.Trip
{
    public class TripDetailDTO : TripDTO
    {
        public List<TripMovementDTO> Movements { get; set; } = new();
        public List<TripLinkedEventDTO> LinkedEvents { get; set; } = new();

        // Las dos lecturas del viaje (Fase 2 de plan-detalle-viaje-montos-propios.md), en la moneda de
        // referencia principal del usuario: lo que le costó (Shares propios + gastos enteramente propios) y
        // lo que se gastó en total (montos íntegros de los movimientos de Evento + esos mismos gastos propios).
        public decimal OwnTotal { get; set; }
        public decimal GrossTotal { get; set; }
    }
}
