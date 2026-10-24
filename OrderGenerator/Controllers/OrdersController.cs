using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Services;

namespace OrderGenerator.Controllers;

public sealed record NewOrderRequest(string simbolo, int tipoOrdem, int quantidade, decimal preco);

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(FixOrderClient fixClient) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] NewOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await fixClient.SendOrderAsync(request.simbolo, request.tipoOrdem == 1 ? '1' : '2', request.quantidade, request.preco, cancellationToken);
            return Ok(reply);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (TimeoutException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { error = "Tempo esgotado aguardando o ExecutionReport da exchange." });
        }
    }
}
