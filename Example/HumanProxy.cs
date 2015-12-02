using System.Runtime.ExceptionServices;
using ActorInterface;
using TypedActorFramework;

namespace Example
{
    // Note: this class is not used.
    // It is used to think about the design of proxy classes.

    public class HumanProxy : IHuman
    {
        private IMailbox<object> mailbox;
        private IActorRuntime runtime;

        public HumanProxy(ActorId<IHuman> id, IActorRuntime runtime)
        {
//            this.id = id;
            this.runtime = runtime;
        }

        #region Implementation of IHuman

        public int Eat(int a, double b, object o, IHuman h)
        {
            var resultMailbox = runtime.CreateMailbox<CallResult<int>>();
            var hep = new HumanEatParams(a, b, o, h, resultMailbox);
            mailbox.Send(hep);
            var callResult = resultMailbox.Receive();

            if (callResult.exception != null)
            {
                ExceptionDispatchInfo.Capture(callResult.exception).Throw();
            }

            return callResult.result;
        }

        #endregion
    }
}