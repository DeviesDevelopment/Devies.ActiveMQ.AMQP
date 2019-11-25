using System;
using System.Threading;
using System.Threading.Tasks;
using Devies.ActiveMQ.AMQP.Models;

namespace Devies.ActiveMQ.AMQP.Services
{
    public interface IQueueReadService<T> : IDisposable
    {
        bool IsConnected { get; }

        Task Connect();

        Task ReadQueue(CancellationToken token, Func<T, Task<MessageAction>> callback);
    }
}