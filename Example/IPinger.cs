using TypedActorInterface;

namespace Example
{
    public interface IPinger : ITypedActor
    {
        void SetDestination(IPrinter printer);
        void Ping();
    }
}