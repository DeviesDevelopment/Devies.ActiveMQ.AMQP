using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devies.ActiveMQ.AMQP.Services
{
    public interface IQueueWriteService<T> : IDisposable
    {
        bool IsConnected { get; }

        Task AddToQueue(IEnumerable<T> data);

        Task Connect();
    }
}