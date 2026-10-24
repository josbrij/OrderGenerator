using System.Collections.Concurrent;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using FixMessage = QuickFix.Message;

namespace OrderGenerator.Services;

public sealed record OrderReply(bool Accepted, string ClOrdId, string ExecType, string OrdStatus, string Message, string? Symbol, decimal? Price, int? Quantity);

public sealed class FixOrderClient : IApplication, IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FixOrderClient> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderReply>> _pending = new();
    private SocketInitiator? _initiator;
    private readonly SessionID _sessionId = new("FIX.4.4", "ORDERGENERATOR", "ORDERACCUMULATOR");

    public FixOrderClient(IConfiguration configuration, ILogger<FixOrderClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }    

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _initiator?.Stop();
        return Task.CompletedTask;
    }

    public async Task<OrderReply> SendOrderAsync(string simbolo, char tipoOrdem, int quantidade, decimal preco, CancellationToken cancellationToken)
    {
        if (_initiator is null || Session.LookupSession(_sessionId) is not { IsLoggedOn: true })
            throw new InvalidOperationException("A sessão FIX com o OrderAccumulator não está conectada.");

        var clOrdId = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<OrderReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[clOrdId] = completion;

        try
        {
            var order = new NewOrderSingle(new ClOrdID(clOrdId), new Symbol(simbolo), new Side(tipoOrdem),
                new TransactTime(DateTime.UtcNow), new OrdType(OrdType.LIMIT));
            order.Set(new OrderQty(quantidade));
            order.Set(new Price(preco));
            order.Set(new TimeInForce(TimeInForce.DAY));

            if (!Session.SendToTarget(order, _sessionId))
                throw new InvalidOperationException("O mecanismo FIX não conseguiu enviar a ordem.");

            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        finally
        {
            _pending.TryRemove(clOrdId, out _);
        }
    }

    public void FromApp(FixMessage message, SessionID sessionId)
    {
        if (message is not ExecutionReport report || !report.IsSetClOrdID())
            return;

        var clOrdId = report.ClOrdID.Value;
        if (!_pending.TryGetValue(clOrdId, out var completion))
            return;

        var execType = report.ExecType.Value;
        var accepted = execType == ExecType.NEW;
        var explanation = report.IsSetText() ? report.Text.Value : (accepted ? "Ordem aceita." : "Ordem rejeitada.");
        completion.TrySetResult(new OrderReply(
            accepted,
            clOrdId,
            accepted ? "New" : "Rejected",
            accepted ? "New" : "Rejected",
            explanation,
            report.IsSetSymbol() ? report.Symbol.Value : null,
            report.IsSetPrice() ? report.Price.Value : null,
            report.IsSetOrderQty() ? (int)report.OrderQty.Value : null));
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        //obter as configurações iniciais pra comunicar com o FIX
        var settingsPath = Path.Combine(AppContext.BaseDirectory, _configuration["Fix:SettingsFile"] ?? "fix-initiator.cfg");
        var settings = new SessionSettings(settingsPath);
        _initiator = new SocketInitiator(this, new FileStoreFactory(settings), settings, new FileLogFactory(settings), new DefaultMessageFactory());
        _initiator.Start();
        return Task.CompletedTask;
    }

    public void OnCreate(SessionID sessionId) { }
    public void OnLogon(SessionID sessionId) => _logger.LogInformation("Sessão FIX conectada: {Session}", sessionId);
    public void OnLogout(SessionID sessionId) => _logger.LogWarning("Sessão FIX desconectada: {Session}", sessionId);
    public void ToAdmin(FixMessage message, SessionID sessionId) { }
    public void ToApp(FixMessage message, SessionID sessionId) { }
    public void FromAdmin(FixMessage message, SessionID sessionId) { }

    public void Dispose()
    {
        _initiator?.Stop();
        _initiator?.Dispose();
    }
}
