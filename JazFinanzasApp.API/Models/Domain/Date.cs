using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Models.Domain
{
    public class Date
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public Date DateField { get; set; }
        
        public int Year { get; set; }
        public int Month { get; set; }  
        public int Day { get; set; }
        public string DayName { get; set; }
        public string MonthName { get; set; }  


    }
}
