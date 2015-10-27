using TypedActorFramework;

namespace Example
{
    public class HumanProxy : IHuman
    {
        private ActorId<IHuman> id;
        private IActorRuntimeInternal runtime;

        public HumanProxy(ActorId<IHuman> id, IActorRuntimeInternal runtime)
        {
            this.id = id;
            this.runtime = runtime;
        }

        #region Implementation of IHuman

        public void Eat(int a, double b, object o, IHuman h)
        {
            var hep = new HumanEatParams(runtime, a, b, o, h);

            runtime.Send(this, hep);
//            hep.Call(this, runtime);
        }

        #endregion
    }
}