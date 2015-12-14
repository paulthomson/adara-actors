using TypedActorInterface;

namespace FailureDetectorExample
{
    public interface INode : ITypedActor
    {
        void Ping(IFailureDetector detector);
    }
}