using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

public class CurrencyExchangeService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly IMemoryCache _cache;

    public CurrencyExchangeService(HttpClient http, IConfiguration configuration, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;

        _apiKey = Environment.GetEnvironmentVariable("CUR_EX_KEY") ?? string.Empty;
        _apiUrl = Environment.GetEnvironmentVariable("CUR_EX_URL") ?? string.Empty;
    }

    public async Task<decimal> ConvertAsync(
    decimal amount,
    string fromCurrency,
    string toCurrency)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var cacheKey = $"fx:{fromCurrency}:{toCurrency}";

        if (!_cache.TryGetValue(cacheKey, out decimal rate))
        {
            var url = $"{_apiUrl}/{_apiKey}/latest/{fromCurrency.ToUpper()}";

            var response =
                await _http.GetFromJsonAsync<ExchangeRateResponse>(url);

            if (response == null ||
                !response.ConversionRates.TryGetValue(
                    toCurrency.ToUpper(),
                    out rate))
            {
                throw new AppException("CUR_EX_API_ERR", "Failed to retrieve exchange rate.", "Exchange rate not found.");
            }

            _cache.Set(cacheKey, rate);
        }

        return amount * rate;
    }

    public async Task<List<CurrencyDto>> GetCurrenciesAsync()
    {
        var url = $"{_apiUrl}/{_apiKey}/codes";

        var response = await _http.GetFromJsonAsync<CurrencyResponse>(url);

        if (response == null)
        {
            throw new AppException(
                "CUR_EX_API_ERR",
                "Failed to retrieve currencies.",
                "Unable to retrieve currencies."
            );
        }

        return response.SupportedCodes
            .Select(x => new CurrencyDto
            {
                Code = x[0],
                Name = x[1]
            })
            .OrderBy(x => x.Code)
            .ToList();
    }
    public class ExchangeRateResponse
    {
        [JsonPropertyName("conversion_rates")]
        public Dictionary<string, decimal> ConversionRates { get; set; } = new();
    }

    public class CurrencyResponse
    {
        [JsonPropertyName("supported_codes")]
        public List<List<string>> SupportedCodes { get; set; } = [];
    }

    public class CurrencyDto
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
    }
}