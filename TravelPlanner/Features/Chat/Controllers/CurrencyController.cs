using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/currency")]
public class CurrencyController : ControllerBase
{
    private readonly CurrencyExchangeService _service;

    public CurrencyController(CurrencyExchangeService service)
    {
        _service = service;
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies()
    {
        var currencies = await _service.GetCurrenciesAsync();

        return Ok(currencies);
    }
}