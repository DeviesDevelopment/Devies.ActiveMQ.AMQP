using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Devies.ActiveMQ.AMQP.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Devies.ActiveMQ.AMQP.Services
{
    public class ReadAMQPService<T> : IQueueReadService<T>
    {
        private readonly string queue;
        private readonly string receiverName;
        private readonly SemaphoreSlim connectSemaphore;
        private readonly QueueOptions options;
        private readonly ILogger<ReadAMQPService<T>> logger;
        private Connection connection;
        private Session session;
        private ReceiverLink receiver = null;

        public ReadAMQPService(QueueOptions options, ILogger<ReadAMQPService<T>> logger)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9\\.]");
            queue = rgx.Replace(options.Queue.ToUpper(), "");

            receiverName = options.ReceiverName;
            connectSemaphore = new SemaphoreSlim(1);
            this.options = options;
            this.logger = logger;
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

        public async Task ReadQueue(CancellationToken token, Func<T, Task<MessageAction>> callback)
        {
            if (receiver != null)
            {
                throw new Exception("Someone already reading, create a new instance");
            }

            await Connect();
            receiver = new ReceiverLink(session, receiverName, $"{queue}");

            token.Register(Close);

            while (receiver != null && !token.IsCancellationRequested)
            {
                try
                {
                    var message = await receiver.ReceiveAsync();
                    if (message != null)
                    {
                        try
                        {
                            var cardInfo = JsonConvert.DeserializeObject<T>(message.Body.ToString());

                            var action = await callback(cardInfo);

                            switch (action)
                            {
                                case MessageAction.ACCEPT:
                                    receiver.Accept(message);
                                    break;
                                case MessageAction.RELEASE:
                                    receiver.Reject(message);
                                    break;
                                default:
                                    receiver.Reject(message);
                                    break;
                            }
                        }
                        catch (JsonReaderException)
                        {
                            receiver.Reject(message);
                        }
                        catch (JsonSerializationException)
                        {
                            receiver.Reject(message);
                        }
                    }
                }
                catch
                {
                    if (receiver != null && !token.IsCancellationRequested)
                    {
                        Close();
                        await Connect();
                        receiver = new ReceiverLink(session, receiverName, $"{queue}");
                    }
                }
            }

            Close();
        }


        private void Close()
        {
            var r = receiver;
            receiver = null;

            if (r != null && !r.IsClosed)
            {
                r.Close();
            }

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