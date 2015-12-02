using TypedActorInterface;

namespace Example
{
    public interface IHuman : ITypedActor
    {
        int Eat(int a, double b, object o, IHuman h);
    }
}