var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString = hostContext.Configuration.GetConnectionString("feeds");
        
        var feedQueueClient = new QueueClient(connectionString, "feeds");
        feedQueueClient.CreateIfNotExists();
        services.AddSingleton(feedQueueClient);
        
        services.AddScoped<FeedIngestionHandler>();
        services.AddSingleton(x => new BlobServiceClient(connectionString));
        
        services.AddHostedService<Worker>();

    })
    .Build();

await host.RunAsync();
