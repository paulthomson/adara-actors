namespace ActorInterface
{
    public interface IMailbox<T>
    {
        void Send(T msg);
        T Receive();
    }
}