namespace Devies.ActiveMQ.AMQP.Models
{
    public enum MessageAction
    {
        RELEASE, // Retry indefinitely
        REJECT,  // Invalid, won't process
        ACCEPT   // Done, drop message
    }
}