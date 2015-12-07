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

        public IMailbox<object> resultMailbox;

        public int result;
        public Exception exception;

        public HumanEatParams(
            int a,
            double b,
            object o,
            IHuman h,
            IMailbox<object> resultMailbox)
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
                result = ((IHuman) t).Eat(ref a, b, o, h);
                resultMailbox.Send(this);
            }
            catch (Exception ex)
            {
                exception = ex;
                resultMailbox.Send(this);
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