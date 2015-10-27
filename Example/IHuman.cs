using TypedActorInterface;

namespace Example
{
    public interface IHuman : ITypedActor
    {
        void Eat(int a, double b, object o, IHuman h);
    }
}