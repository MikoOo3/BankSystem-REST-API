namespace BankSystem.DataAccess.DTOs
{
    public class CurrencyDTO
    {
        public int Id { get; set; }
        public string Country { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal Rate { get; set; }

        public CurrencyDTO(int id, string country, string currencyCode, string currencyName, decimal rate)
        {
            Id = id;
            Country = country;
            CurrencyCode = currencyCode;
            CurrencyName = currencyName;
            Rate = rate;
        }
    }
}