using System;
using System.Collections.Generic;
using System.Text;

namespace BankDataAccessLayer
{
    public class Currency
    {
        public int Id { get; set; }
        public string Country { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }
}