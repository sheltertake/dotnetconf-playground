internal class FeedIngestionHandler
{
    private readonly BlobServiceClient _client;

    public FeedIngestionHandler(BlobServiceClient client)
    {

        _client = client;
    }
    internal async Task HandleIngestionAsync(NewFeedRequested feed, CancellationToken stoppingToken)
    {
        var containerClient = _client.GetBlobContainerClient("feeds");
        containerClient.CreateIfNotExists(PublicAccessType.Blob);
        var blobClient = containerClient.GetBlobClient(feed.Guid.ToString());

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(feed)));
        var _ = await blobClient.UploadAsync(ms,
            //new BlobHttpHeaders { ContentType = "application/json" },
           cancellationToken: stoppingToken);
    }
}
