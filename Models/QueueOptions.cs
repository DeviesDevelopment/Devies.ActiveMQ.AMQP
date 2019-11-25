namespace Devies.ActiveMQ.AMQP.Models
{
    public class QueueOptions
    {
        public string Url { get; set; }

        public string Queue { get; set; }

        public string User { get; set; }

        public string Port { get; set; }

        public string Pass { get; set; }

        public string ReceiverName { get; set; }
    }
}