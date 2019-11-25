using System;
using System.Threading;
using System.Threading.Tasks;
using Devies.ActiveMQ.AMQP.Models;
using Devies.ActiveMQ.AMQP.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devies.ActiveMQ.AMQP
{
    public abstract class BaseQueueTask<T> : BackgroundService, IDisposable
    {
        private readonly ILogger<BaseQueueTask<T>> logger;
        private readonly IServiceProvider serviceProvider;
        private IQueueReadService<T> currentQueueReader;
        private int fails = 0;


        protected BaseQueueTask(
            ILogger<BaseQueueTask<T>> logger,
            IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected abstract Task<MessageAction> Execute(IServiceProvider serviceProvider, T message);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var appLifetime = scope.ServiceProvider.GetRequiredService<IApplicationLifetime>();
                appLifetime.ApplicationStopping.Register(OnStopping);
                appLifetime.ApplicationStopped.Register(OnStopping);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        currentQueueReader = scope.ServiceProvider.GetRequiredService<IQueueReadService<T>>();
                        await currentQueueReader.ReadQueue(stoppingToken, async message =>
                        {
                            fails = 0;
                            try
                            {
                                return await Execute(serviceProvider, message);
                            }
                            catch (Exception e)
                            {
                                logger.LogError(e, e.Message);
                                return MessageAction.REJECT;
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        fails = Math.Min(300, Math.Max(1, fails * 2));
                        await Task.Delay(Math.Min(60000, fails * 200), stoppingToken);
                        logger.LogError(e, e.Message);
                    }
                }
            }
        }

        private void OnStopping()
        {
            currentQueueReader?.Dispose();
        }
    }
}