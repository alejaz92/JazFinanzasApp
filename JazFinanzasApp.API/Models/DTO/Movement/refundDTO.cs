﻿namespace JazFinanzasApp.API.Models.DTO.Movement
{
    public class RefundDTO
    {
        public int AccountId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}