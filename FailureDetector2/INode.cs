using TypedActorInterface;

namespace FailureDetector2
{
    public interface INode : ITypedActor
    {
        void Ping(IFailureDetector detector);
    }
}