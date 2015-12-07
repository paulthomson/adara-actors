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

        public int Eat(ref int a, double b, object o, IHuman h)
        {
            var resultMailbox = runtime.CreateMailbox<object>();
            var hep = new HumanEatParams(a, b, o, h, resultMailbox);
            mailbox.Send(hep);
            hep = (HumanEatParams) resultMailbox.Receive();

            a = hep.a;

            var ex = hep.exception;

            if (ex != null)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            return hep.result;
        }

        #endregion

        public void TestMethod(out MyStruct s1, MyStruct s2)
        {
            s1 = s2;
        }
        
    }

    public struct MyStruct
    {
        private int a;
        private int b;
        private int c;
    }
}