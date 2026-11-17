namespace BankSystem.DataAccess.DTOs
{
    public class CurrencyConversionResultDTO
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal FromRate { get; set; }
        public decimal ToRate { get; set; }
        public decimal ConvertedAmount { get; set; }
    }
}