using System.ComponentModel.DataAnnotations;

namespace BankSystem.DataAccess.DTOs
{
    public class UpdateCurrencyRateDTO
    {
        [Required]
        [Range(0.000001, double.MaxValue, ErrorMessage = "Exchange rate must be greater than zero.")]
        public decimal NewRate { get; set; }
    }
}