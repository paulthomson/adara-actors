# Adara Actors

Adara actors is a statically-typed actors framework for C#. The focus is currently on actors that run within a single process.

## Motivation

Libraries like P# and Akka typically use dynamically-typed actors (which we refer to as untyped actors). That is, there are no static checks or hints about what the type of an actor is. For example:

```c#
// We can see that the type of the actor will be `Human` in this case
// but this information is not available to the compiler nor to the IDE. 
MachineId m = CreateMachine(typeof(Human), "Tim");

// What messages can be sent to `m`? What is its "interface"?

Send(m, new EatMsg("Pizza", 2));

// No static check that `EatMsg` can be accepted by `m`.

```

The compiler and IDE cannot help us. It is also hard to find the message handlers of `EatMsg` as the IDE cannot relate this class to the message handlers. Instead, you must search for all uses of `EatMsg` until you find the appropriate annotation:

```
[OnEventDoAction(typeof(EatMsg), nameof(HandleEat))]
```

From there, you can jump to the `HandleEat` method.

A further potential issue is that your code is not portable; it is dependent on the P# classes and so is somewhat "locked-in" to the framework that you have chosen, even though your code is really just message passing. It would be ideal if we could write code that is portable; i.e. independent of the chosen runtime/framework such that it could run on different runtimes (e.g. P#, Akka, something else) without changes.

## Solution

With Adara's statically-typed actors, we use interfaces.

```c#
// IHuman is an interface. A new instance of Human provides the handlers and private state of the actor.
IHuman humanProxy = 
  typedRuntime.Create<IHuman>(new Human());

// Send messages to the IHuman actor (implemented by the Human object) by invoking methods.
humanProxy.Eat("Pizza", 2);

```

In the above, the runtime type of `humanProxy` is `HumanProxy`; this type is generated at runtime (once) using `System.Reflection.Emit`. The generated `Eat` method packs up the parameters into an object of type `EatMsg`; this type is also generated. The `EatMsg` is then sent to the actor. When the runtime processes the message, it is unpacked and the `Eat` method of the `Human` object is invoked with the appropriate parameters.

The IDE autocompletes the methods of `IHuman` and gives an error if we type a method that does not exist.
We can jump to the `IHuman` interface to see exactly what messages it can receive (i.e. its methods):

```c#
public interface IHuman : ITypedActor
{
    void Eat(string food, int nourishment);    
    void Run(int distanceInMeters);
}

```

Since `IHuman` is simply an interface, all existing IDE features and static tools continue to work. Thus, we can directly navigate to all implementations of `Eat`. In this case, there is only one:

```c#
public class Human : IHuman
{
    private string name;
    private int health;
    
    public void Eat(string food, int nourishment)
    {
        Console.WriteLine($"{name} just ate {food}");
        health += nourishment;
    }
    
    // ...
}
```

The typed actor runtime (that allows us to create typed actors) is just an interface; we provide an implementation that depends on an untyped actor runtime interface. We provide two straightforward actor runtime implementations (one for production, one for systematic concurrency testing), but other implementations would be straightforward to implement. E.g. an implementation that uses P#. The following shows how the projects/assemblies relate:

```
Your code
  |
  | uses
  |
  v
Typed Actor Runtime Interfaces
  ^
  |
  | implements
  |
Typed Actor Runtime
  |
  | uses
  |
  v
Untyped Actor Runtime Interfaces
  ^                            ^
  |                            | 
  | implements                 | implements
  |                            |
Untyped Runtime (production)   |
                               |
             Untyped Runtime (systematic concurrency testing)

```

## Example

See [an example](Example/Program.cs) showing both the untyped and typed actors, and systematic concurrency testing.

