namespace JazFinanzasApp.API.Business.DTO.SharedEvent.Import
{
    public class SharedEventImportMemberDTO
    {
        public string Name { get; set; } = string.Empty;
        public int? SuggestedPersonId { get; set; }
        public string? SuggestedPersonName { get; set; }
    }

    public class SharedEventImportCategoryDTO
    {
        public string Name { get; set; } = string.Empty;
        public int? SuggestedTransactionClassId { get; set; }
        public string? SuggestedTransactionClassName { get; set; }
    }

    public class SharedEventImportMemberDeltaDTO
    {
        public string MemberName { get; set; } = string.Empty;
        public decimal Delta { get; set; }
    }

    public class SharedEventImportSuggestedMatchDTO
    {
        public int? TransactionId { get; set; }
        public int? CardTransactionId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public class SharedEventImportRowDTO
    {
        public int RowIndex { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int? AssetId { get; set; }
        public bool IsPayment { get; set; }
        public bool Unsupported { get; set; }
        public string? PayerMemberName { get; set; }
        public string? ReceiverMemberName { get; set; }
        public List<SharedEventImportMemberDeltaDTO> MemberDeltas { get; set; } = new();
        public List<SharedEventImportSuggestedMatchDTO> SuggestedMatches { get; set; } = new();
    }

    public class SharedEventImportBalanceRowDTO
    {
        public string Currency { get; set; } = string.Empty;
        public List<SharedEventImportMemberDeltaDTO> MemberBalances { get; set; } = new();
    }

    public class SharedEventImportParseResultDTO
    {
        public List<SharedEventImportMemberDTO> Members { get; set; } = new();
        public List<SharedEventImportCategoryDTO> Categories { get; set; } = new();
        public List<SharedEventImportRowDTO> Rows { get; set; } = new();
        public List<SharedEventImportBalanceRowDTO> BalanceRows { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
