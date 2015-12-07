using TypedActorInterface;

namespace Example
{
    public interface IHuman : ITypedActor
    {
        int Eat(ref int a, double b, object o, IHuman h);
    }
}