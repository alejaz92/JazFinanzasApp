namespace JazFinanzasApp.API.Business.DTO.Trip
{
    // Movimiento del viaje unificado: transacción de cuenta u consumo de tarjeta
    public class TripMovementDTO
    {
        public int Id { get; set; } // id de la Transaction o del CardTransaction según Origin
        public string Origin { get; set; } // ACCOUNT / CARD
        public DateTime Date { get; set; }
        public string? TransactionClass { get; set; }
        public string? Detail { get; set; }
        public decimal Amount { get; set; } // positivo; para CARD es el TotalAmount (devengado)
        public string Asset { get; set; }
        public string AssetSymbol { get; set; }

        // Parte propia según el Evento Compartido vinculado (Shares con PersonId == null). Null si el
        // movimiento no pertenece a ningún evento (incluye sugerencias y búsqueda de asociables).
        public decimal? OwnAmount { get; set; }
        public bool IsShared { get; set; }
        public int? SharedEventId { get; set; }
        public List<string>? SharedWith { get; set; }

        // Lo que gastó el grupo en el movimiento de Evento (SharedEventMovement.TotalAmount), no necesariamente
        // igual a Amount: cuando el gasto lo pagó otra persona y el usuario saldó su parte después, Amount es el
        // monto de la transacción de saldo (ya es la parte propia), no el total del grupo — GrossAmount sí lo es.
        public decimal? GrossAmount { get; set; }

        // Nombre de quien pagó, si no fue el usuario (SharedEventMovement.PayerPersonId). Null si pagó el
        // usuario o si el movimiento no está compartido.
        public string? PaidByName { get; set; }
    }
}
