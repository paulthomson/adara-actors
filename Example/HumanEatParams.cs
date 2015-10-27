using TypedActorFramework;
using TypedActorInterface;

namespace Example
{
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

        public void Call(ITypedActor t, IActorRuntimeInternal runtime)
        {
            ((IHuman) t).Eat(a, b, o, runtime.GetActorProxy(h));
        }
    }
}