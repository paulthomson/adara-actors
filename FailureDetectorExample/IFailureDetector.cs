using System.Collections.Generic;
using TypedActorInterface;

namespace FailureDetectorExample
{
    public interface IFailureDetector
    {
        void Init(List<INode> nodes);
        void Pong(INode node);
    }
}