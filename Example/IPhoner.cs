using TypedActorInterface;

namespace Example
{
    public interface IPhoner : ITypedActor
    {
        void Init(int id, IAnswerPhone answerPhone);
        void Go();
    }
}