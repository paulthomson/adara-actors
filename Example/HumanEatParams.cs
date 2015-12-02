using System;
using ActorFramework;
using ActorInterface;
using TypedActorFramework;
using TypedActorInterface;

namespace Example
{

    // Note: this class is not used.
    // It is used to think about the design of parameter classes.

    public class HumanEatParams : ICallable
    {
        public int a;
        public double b;
        public object o;
        public IHuman h;

        public IMailbox<CallResult<int>> resultMailbox;


        public HumanEatParams(
            int a,
            double b,
            object o,
            IHuman h,
            IMailbox<CallResult<int>> resultMailbox)
        {
            this.a = a;
            this.b = b;
            this.o = o;
            this.h = h;
            this.resultMailbox = resultMailbox;
        }

        public void Call(ITypedActor t)
        {
            try
            {
                var res = ((IHuman) t).Eat(a, b, o, h);
                var callRes = new CallResult<int>();
                callRes.result = res;
                resultMailbox.Send(callRes);
            }
            catch (Exception ex)
            {
                var callRes = new CallResult<int>();
                callRes.exception = ex;
                resultMailbox.Send(callRes);
            }
        }

        #region Overrides of Object

        public override string ToString()
        {
            return "HumanEat";
        }

        #endregion
    }
}