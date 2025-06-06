﻿using System.Reflection.Metadata;

namespace JazFinanzasApp.API.Business.DTO.Report
{
    public class TotalsBalanceDTO
    {
        public string Asset { get; set; }
        public string Symbol { get; set; }
        public string Color { get; set; }
        public decimal Balance { get; set; }
    }

    public class TotalBalanceResult
    {
        public decimal? Total { get; set; }
    }
}
