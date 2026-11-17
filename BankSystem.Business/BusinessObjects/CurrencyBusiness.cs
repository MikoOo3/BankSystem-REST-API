using BankDataAccessLayer;
using BankSystem.DataAccess.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankBusinessLayer
{
    public class CurrencyBusiness
    {
        public static async Task<List<CurrencyDTO>> GetAllCurrenciesAsync(BankSystemDbContext context)
        {
            return await context.Currencies
                .AsNoTracking()
                .OrderBy(c => c.Country)
                .Select(c => new CurrencyDTO(c.Id, c.Country, c.CurrencyCode, c.CurrencyName, c.Rate))
                .ToListAsync();
        }

        public static async Task<CurrencyDTO?> FindByIdAsync(BankSystemDbContext context, int id)
        {
            return await context.Currencies
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CurrencyDTO(c.Id, c.Country, c.CurrencyCode, c.CurrencyName, c.Rate))
                .FirstOrDefaultAsync();
        }

        public static async Task<CurrencyDTO?> FindByCodeAsync(BankSystemDbContext context, string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode)) return null;

            string code = currencyCode.Trim().ToUpper();

            return await context.Currencies
                .AsNoTracking()
                .Where(c => c.CurrencyCode.ToUpper() == code)
                .Select(c => new CurrencyDTO(c.Id, c.Country, c.CurrencyCode, c.CurrencyName, c.Rate))
                .FirstOrDefaultAsync();
        }

        public static async Task<CurrencyDTO?> FindByCountryAsync(BankSystemDbContext context, string country)
        {
            if (string.IsNullOrWhiteSpace(country)) return null;

            string target = country.Trim().ToUpper();

            return await context.Currencies
                .AsNoTracking()
                .Where(c => c.Country.ToUpper() == target)
                .Select(c => new CurrencyDTO(c.Id, c.Country, c.CurrencyCode, c.CurrencyName, c.Rate))
                .FirstOrDefaultAsync();
        }

        public static async Task<CurrencyDTO?> FindByNameAsync(BankSystemDbContext context, string currencyName)
        {
            if (string.IsNullOrWhiteSpace(currencyName)) return null;

            string target = currencyName.Trim().ToUpper();

            return await context.Currencies
                .AsNoTracking()
                .Where(c => c.CurrencyName.ToUpper() == target)
                .Select(c => new CurrencyDTO(c.Id, c.Country, c.CurrencyCode, c.CurrencyName, c.Rate))
                .FirstOrDefaultAsync();
        }

        public static async Task<(CurrencyConversionResultDTO? Result, string? ErrorMessage)> CalculateExchangeAsync(BankSystemDbContext context,
            string fromCode, string toCode, decimal amount)
        {
            if (amount <= 0)
                return (null, "Amount must be greater than zero.");

            var fromCurrency = await FindByCodeAsync(context, fromCode);
            if (fromCurrency == null)
                return (null, $"Source currency '{fromCode}' not found.");

            var toCurrency = await FindByCodeAsync(context, toCode);
            if (toCurrency == null)
                return (null, $"Target currency '{toCode}' not found.");

            decimal convertedAmount = fromCurrency.CurrencyCode == toCurrency.CurrencyCode
                ? amount
                : Math.Round((amount / fromCurrency.Rate) * toCurrency.Rate, 2);

            var result = new CurrencyConversionResultDTO
            {
                FromCurrency = fromCurrency.CurrencyCode,
                ToCurrency = toCurrency.CurrencyCode,
                OriginalAmount = amount,
                FromRate = fromCurrency.Rate,
                ToRate = toCurrency.Rate,
                ConvertedAmount = convertedAmount
            };

            return (result, null);
        }

        public static async Task<(bool Success, string? ErrorMessage)> UpdateRateAsync(BankSystemDbContext context,
            string currencyCode, decimal newRate, Guid performedByUserId)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return (false, "Currency code is required.");

            if (newRate <= 0)
                return (false, "Exchange rate must be greater than zero.");

            string code = currencyCode.Trim().ToUpper();

            if (code == "USD" && newRate != 1.0m)
                return (false, "Base currency (USD) rate must remain 1.0.");

            var matchingCurrencies = await context.Currencies
                .Where(c => c.CurrencyCode.ToUpper() == code)
                .ToListAsync();

            if (matchingCurrencies.Count == 0)
                return (false, $"Currency '{code}' not found.");

            decimal oldRate = matchingCurrencies.First().Rate;

            foreach (var currency in matchingCurrencies)
            {
                currency.Rate = newRate;
            }

            var auditLog = new AuditLog
            {
                UserId = performedByUserId,
                Action = "UpdateCurrencyRate",
                OldValues = JsonSerializer.Serialize(new { CurrencyCode = code, Rate = oldRate }),
                NewValues = JsonSerializer.Serialize(new { CurrencyCode = code, Rate = newRate }),
                Timestamp = DateTime.UtcNow
            };

            await context.AuditLogs.AddAsync(auditLog);
            await context.SaveChangesAsync();

            return (true, null);
        }
    }
}