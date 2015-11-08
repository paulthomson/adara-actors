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
        public ActorId<IHuman> h;


        public HumanEatParams(
            IActorRuntimeInternal runtime,
            int a,
            double b,
            object o,
            IHuman h)
        {
            this.a = a;
            this.b = b;
            this.o = o;
            this.h = runtime.GetActorId(h);
        }

        public void Call(ITypedActor t)
        {
            ((IHuman) t).Eat(a, b, o, null);
        }

        #region Overrides of Object

        public override string ToString()
        {
            return "HumanEat";
        }

        #endregion
    }
}