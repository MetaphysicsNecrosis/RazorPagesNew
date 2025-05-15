using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RazorPagesNew.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RazorPagesNew.Services.Background
{
    public class RoleSynchronizationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RoleSynchronizationBackgroundService> _logger;
        private readonly TimeSpan _syncInterval;

        public RoleSynchronizationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RoleSynchronizationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _syncInterval = TimeSpan.FromMinutes(2); // Синхронизация каждые 30 минут
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Служба синхронизации ролей запущена");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Начало синхронизации ролей");

                    // Создаем новый scope для получения сервисов
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var roleSyncService = scope.ServiceProvider.GetRequiredService<IRoleSynchronizationService>();

                        // Синхронизируем роли
                        await roleSyncService.SynchronizeRolesAsync();

                        _logger.LogInformation("Синхронизация ролей завершена успешно");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при синхронизации ролей");
                }

                // Ждем указанный интервал перед следующей синхронизацией
                await Task.Delay(_syncInterval, stoppingToken);
            }

            _logger.LogInformation("Служба синхронизации ролей остановлена");
        }
    }
}