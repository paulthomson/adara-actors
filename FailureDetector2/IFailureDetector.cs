using System.Collections.Generic;
using TypedActorInterface;

namespace FailureDetector2
{
    public interface IFailureDetector : ITypedActor
    {
        MsgRes Init(List<INode> nodes);
        MsgRes Pong(INode node);
    }
}