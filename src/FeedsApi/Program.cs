var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("feeds");
builder.Services.AddSingleton(new QueueClient(connectionString, "feeds"));

builder.Services.AddSingleton<FastStorageService>();
builder.Services.AddSingleton<BlobService>();
builder.Services.AddSingleton(x => new BlobServiceClient(connectionString));
builder.Services.AddHostedService<FastStorageServiceWorker>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.MapPost("/", async (QueueClient queueClient, Feed feed, CancellationToken cancellationToken) =>
{
    await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    await queueClient.SendMessageAsync(new BinaryData(feed), cancellationToken: cancellationToken);
});

app.MapGet("/slow", async (BlobService blobService, CancellationToken cancellationToken) =>
{
    var feed = new Feed { Name = "slow" };

    await blobService.UploadAsync(feed, cancellationToken);

    return feed.Guid.ToString();
});

app.MapGet("/fast", (FastStorageService storage, CancellationToken cancellationToken) =>
{
    var feed = new Feed { Name = "slow" };

    storage.Writer.TryWrite(feed);

    return feed.Guid.ToString();
});

app.Run();

public record Feed
{
    public Guid Guid { get; } = Guid.NewGuid();
    public string? Name { get; init; }
}

internal class BlobService
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<BlobService> _logger;

    public BlobService(BlobServiceClient client, ILogger<BlobService> logger)
    {
        _client = client;
        _logger = logger;
    }

    internal async Task UploadAsync(Feed feed, CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = _client.GetBlobContainerClient("channels");
            containerClient.CreateIfNotExists(PublicAccessType.Blob);
            var blobClient = containerClient.GetBlobClient(feed.Guid.ToString());

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(feed)));
            var _ = await blobClient.UploadAsync(ms,
               cancellationToken: cancellationToken);

        }
        catch(TaskCanceledException)
        {
            _logger.LogInformation("Task cancelled at {time}", DateTimeOffset.Now);
        }
        
    }
}

internal class FastStorageService
{
    private readonly Channel<Feed> _channel = Channel.CreateUnbounded<Feed>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    public ChannelWriter<Feed> Writer => _channel.Writer;
    public ChannelReader<Feed> Reader => _channel.Reader;
}

internal class FastStorageServiceWorker : BackgroundService
{
    private readonly ILogger<FastStorageServiceWorker> _logger;
    private readonly FastStorageService _fastStorageService;
    private readonly BlobService _blobService;

    public FastStorageServiceWorker(ILogger<FastStorageServiceWorker> logger, FastStorageService fastStorageService, BlobService blobService)
    {
        _logger = logger;
        _fastStorageService = fastStorageService;
        _blobService = blobService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _fastStorageService.Reader.WaitToReadAsync(stoppingToken))
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            if (_fastStorageService.Reader.TryRead(out var item))
            {
                await _blobService.UploadAsync(item, stoppingToken);
            }
        }
    }
}


record NewFeedRequested
{
    public Guid Guid { get; init; }
    public string? Name { get; init; }
}
