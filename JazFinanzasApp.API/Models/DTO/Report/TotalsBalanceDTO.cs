﻿namespace JazFinanzasApp.API.Models.DTO.Report
{
    public class TotalsBalanceDTO
    {
        public string Asset { get; set; }
        public decimal Balance { get; set; }
    }

    public class TotalBalanceResult
    {
        public decimal? Total { get; set; }
    }
}