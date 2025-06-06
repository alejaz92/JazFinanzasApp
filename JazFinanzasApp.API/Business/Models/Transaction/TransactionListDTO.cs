﻿namespace JazFinanzasApp.API.Business.DTO.Transaction
{
    public class TransactionListDTO
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public int PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetSymbol { get; set; }
        public DateTime Date { get; set; }
        public string MovementType { get; set; }
        public int? TransactionClassId { get; set; }
        public string? TransactionClassName { get; set; }
        public string Detail { get; set; }
        public decimal Amount { get; set; }
    }
}
