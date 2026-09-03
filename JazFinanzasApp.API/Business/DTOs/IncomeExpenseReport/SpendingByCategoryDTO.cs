namespace JazFinanzasApp.API.Business.DTO.IncomeExpenseReport
{
    public class CategoryDetailDTO
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }

        // Ascendente, últimos meses (mini-línea de la fila).
        public List<decimal> MonthlyTrend { get; set; } = new();

        public int RankCurrent { get; set; }

        // Null cuando no hay mes anterior con el que compararse (D-B).
        public int? RankPrevious { get; set; }
    }

    // Un rubro es una categoría con hijas (D-2); una categoría sin padre y sin hijas es su propio
    // grupo — hoy ninguna categoría real está clasificada bajo un rubro (ver revisión de D-1/D-2,
    // sección 10 del plan), así que en la práctica todos los grupos arrancan siendo de una sola
    // categoría, hasta que se retome esa clasificación.
    public class CategoryGroupDTO
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public decimal Amount { get; set; }
        public List<CategoryDetailDTO> Categories { get; set; } = new();
    }

    public class SpendingByCategoryDTO
    {
        public DateTime Month { get; set; }
        public List<CategoryGroupDTO> Groups { get; set; } = new();
    }
}
