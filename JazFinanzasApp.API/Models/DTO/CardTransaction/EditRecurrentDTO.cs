﻿namespace JazFinanzasApp.API.Models.DTO.CardTransaction
{
    public class EditRecurrentDTO
    {
        public bool isUpdate { get; set; }
        public DateTime newDate { get; set; }
        public decimal? newAmount { get; set; }
        
    }
}
