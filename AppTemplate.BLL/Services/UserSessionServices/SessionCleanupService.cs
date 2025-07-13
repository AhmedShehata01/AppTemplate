using System;
using System.Threading;
using System.Threading.Tasks;
using AppTemplate.DAL.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppTemplate.BLL.Services.UserSessionServices
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly IConfiguration _configuration;

        private readonly TimeSpan _cleanupInterval;
        private readonly TimeSpan _maxSessionAge;

        public SessionCleanupService(
            IServiceProvider serviceProvider,
            ILogger<SessionCleanupService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // قراءة الإعدادات من appsettings.json
            var cleanupIntervalInMinutes = _configuration.GetValue<int>("Session:CleanupIntervalInMinutes", 10);
            var maxSessionAgeInMinutes = _configuration.GetValue<int>("Session:MaxSessionAgeInMinutes", 10080);

            _cleanupInterval = TimeSpan.FromMinutes(cleanupIntervalInMinutes);
            _maxSessionAge = TimeSpan.FromMinutes(maxSessionAgeInMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ SessionCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupSessions(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error during session cleanup.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        private async Task CleanupSessions(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            var now = DateTime.UtcNow;

            // الجلسات التي تم تسجيل خروجها (عادي أو إجباري)
            var endedSessions = await dbContext.UserSessions
                .Where(s => s.IsLoggedOut || s.ForceLoggedOut)
                .ToListAsync(cancellationToken);

            if (endedSessions.Any())
            {
                dbContext.UserSessions.RemoveRange(endedSessions);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"🗑 Deleted {endedSessions.Count} logged-out sessions.");
            }

            // الجلسات القديمة غير المسجلة خروج
            var oldSessions = await dbContext.UserSessions
                .Where(s => !s.IsLoggedOut
                            && !s.ForceLoggedOut
                            && s.LastActivityAt < now.Subtract(_maxSessionAge))
                .ToListAsync(cancellationToken);

            if (oldSessions.Any())
            {
                dbContext.UserSessions.RemoveRange(oldSessions);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"🗑 Deleted {oldSessions.Count} expired inactive sessions.");
            }
        }
    }
}
