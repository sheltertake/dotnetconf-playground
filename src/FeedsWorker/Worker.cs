internal class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly QueueClient _queueClient;
    private readonly IServiceProvider _serviceProvider;
    public Worker(ILogger<Worker> logger, QueueClient queueClient, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _queueClient = queueClient;
        _serviceProvider = serviceProvider;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var retrievedMessage = await _queueClient.ReceiveMessageAsync(cancellationToken: stoppingToken);
            if (retrievedMessage.Value != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<FeedIngestionHandler>();

                try
                {
                    var ret = retrievedMessage.Value.Body.ToObjectFromJson<NewFeedRequested>();
                    await handler.HandleIngestionAsync(ret, stoppingToken);
                }
                catch (Exception)
                {
                    _logger.LogError("Worker failrd with {time}", DateTimeOffset.Now);
                }

                await _queueClient.DeleteMessageAsync(retrievedMessage.Value.MessageId,
                    retrievedMessage.Value.PopReceipt, stoppingToken);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}


record NewFeedRequested
{
    public Guid Guid { get; init; }
    public string? Name { get; init; }
}
