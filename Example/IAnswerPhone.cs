using ActorInterface;
using TypedActorInterface;

namespace Example
{
    public interface IAnswerPhone : ITypedActor
    {
        void LeaveMessage(string name, string message);
        void CheckMessages(IMailbox<string> res);

        string CheckMessagesSync(int a, out int b, string t);
    }
}