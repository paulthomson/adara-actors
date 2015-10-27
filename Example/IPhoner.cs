using TypedActorInterface;

namespace Example
{
    public interface IPhoner : ITypedActor
    {
        void SetAnswerPhone(IAnswerPhone answerPhone);
        void Go();
    }
}