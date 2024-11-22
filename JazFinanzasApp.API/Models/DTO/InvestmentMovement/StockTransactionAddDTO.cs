﻿namespace JazFinanzasApp.API.Models.DTO.InvestmentMovement
{
    public class StockTransactionAddDTO
    {
        public DateTime Date { get; set; }
        public string Environment { get; set; }
        public string AssetType { get; set; }
        public string StockTransactionType { get; set; }
        public string CommerceType { get; set; }
        public int? ExpenseAssetId { get; set; }
        public int? ExpenseAccountId { get; set; }
        public decimal? ExpenseQuantity { get; set; }
        public decimal? ExpenseQuotePrice { get; set; }
        public int? IncomeAssetId { get; set; }
        public int? IncomeAccountId { get; set; }
        public decimal? IncomeQuantity { get; set; }
        public decimal? IncomeQuotePrice { get; set; }
    }
}
