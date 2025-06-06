﻿namespace JazFinanzasApp.API.Business.DTO.InvestmentTransaction
{
    public class StockTransactionListDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string AssetType { get; set; }
        public string StockMovementType { get; set; }
        public string CommerceType { get; set; }
        public string? ExpenseAsset { get; set; }
        public string? ExpenseAccount { get; set; }
        public string? ExpensePortfolio { get; set; }
        public decimal? ExpenseQuantity { get; set; }
        public decimal? ExpenseQuotePrice { get; set; }
        public string? IncomeAsset { get; set; }
        public string? IncomeAccount { get; set; }
        public string? IncomePortfolio { get; set; }
        public decimal? IncomeQuantity { get; set; }
        public decimal? IncomeQuotePrice { get; set; }
    }
}
