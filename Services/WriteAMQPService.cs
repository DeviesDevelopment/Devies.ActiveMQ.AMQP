using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Devies.ActiveMQ.AMQP.Models;
using Newtonsoft.Json;

namespace Devies.ActiveMQ.AMQP.Services
{
 public class WriteAMQPService<T> : IQueueWriteService<T>
    {
        private readonly string queue;
        private readonly string receiverName;
        private readonly SemaphoreSlim connectSemaphore;
        private readonly QueueOptions options;
        private Connection connection;
        private Session session;

        public WriteAMQPService(QueueOptions options)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9\\.]");
            queue = rgx.Replace(options.Queue.ToUpper(), "");

            receiverName = options.ReceiverName;
            connectSemaphore = new SemaphoreSlim(1);
            this.options = options;
        }

        public async Task Connect()
        {
            await connectSemaphore.WaitAsync();
            try
            {
                if (!IsConnected)
                {
                    var a = new Address(options.Url, int.Parse(options.Port), options.User, options.Pass, null, "AMQP");
                    connection = await Connection.Factory.CreateAsync(a);
                    session = new Session(connection);
                }
            }
            finally
            {
                connectSemaphore.Release();
            }
        }

        public bool IsConnected => connection?.IsClosed == false && session?.IsClosed == false;
        
        public async Task AddToQueue(IEnumerable<T> data)
        {
            await Connect();

            var target = new Target
            {
                Address = queue.IndexOf("topic", StringComparison.InvariantCultureIgnoreCase) == 0
                    ? $"topic://{queue}"
                    : $"queue://{queue}"
            };

            SenderLink sender = new SenderLink(session, receiverName, target, null);

            foreach (var d in data)
            {
                var message = new Message(JsonConvert.SerializeObject(d))
                {
                    Properties = new Properties { CreationTime = DateTime.UtcNow },
                };

                await sender.SendAsync(message);
            }

            await sender.CloseAsync();
        }

        private void Close()
        {
            var s = session;
            session = null;

            if (s != null && !s.IsClosed)
            {
                s.Close();
            }

            var c = connection;
            connection = null;

            if (c != null && !c.IsClosed)
            {
                c.Close();
            }
        }

        public void Dispose()
        {
            Close();
        }

    }
}