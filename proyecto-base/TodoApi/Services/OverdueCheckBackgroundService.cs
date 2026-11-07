namespace TodoApi.Services
{
    public class OverdueCheckBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OverdueCheckBackgroundService> _logger;

        public OverdueCheckBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<OverdueCheckBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var minutes = _configuration.GetValue("OverdueCheck:IntervalMinutes", 1.0);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // El BackgroundService vive toda la aplicación (singleton): los servicios Scoped,
                    // como el DbContext, se piden dentro de un scope nuevo en cada revisión.
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<OverdueNotificationService>();

                    var count = await service.NotifyOverdueAsync(cancellationToken: stoppingToken);
                    _logger.LogInformation("Overdue check: {Count} task(s) notified.", count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Overdue check failed.");
                }
            }
        }
    }
}
