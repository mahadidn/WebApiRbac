using WebApiRbac.Domain.Interfaces;

namespace WebApiRbac.Infrastructure.BackgroundJobs
{
    public class TokenCleanupService : BackgroundService
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly int _daysToKeep;

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _daysToKeep = configuration.GetValue<int>("SecurityPolicy:RefreshTokenRetentionDays", 30);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Token Cleanup Service is enabled");

            // gunakan PeriodicTimer untuk inverval waktu (misal: jalan setiap 24 jam)
            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

            try
            {
                // looping ini akan terus berjalan di background menunggu timer berdetak
                while(await timer.WaitForNextTickAsync(cancellationToken))
                {
                    _logger.LogInformation("Starting the process to clear expired Refresh Tokens...");

                    // BackgroundService adalah Singleton, sedangkan DbContext adalah Scoped.
                    // harus membuat Scope baru secara manual untuk memanggil Repository.
                    using var scope = _serviceProvider.CreateScope();
                    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                    await userRepository.DeleteOldRefreshTokensAsync(_daysToKeep);

                    _logger.LogInformation("The cleaning process is complete.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("The Token Cleanup Service has been stopped.");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "A fatal error occurred in the Token Cleanup Service.");
            }

        }

    }
}
