using System.Text.Json.Serialization;

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