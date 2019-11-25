using Devies.ActiveMQ.AMQP.Models;
using Devies.ActiveMQ.AMQP.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devies.ActiveMQ.AMQP.Extensions
{
    public static class IServiceCollectionExtension
    {
        public static void AddQueueReader<R, T>(this IServiceCollection services, QueueOptions options)
            where T : BackgroundService
        {
            services.AddTransient<IQueueReadService<R>>((s) =>
                new ReadAMQPService<R>(options, s.GetRequiredService<ILogger<ReadAMQPService<R>>>()));
            services.AddSingleton<T>();
            services.AddHostedService<T>();
        }
    }
}