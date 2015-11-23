
using System;
using System.Threading;
using System.Threading.Tasks;
using ActorFramework;
using ActorInterface;
using ActorTestingFramework;
using TypedActorFramework;
using TypedActorInterface;

namespace Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            IActorRuntime runtime = new SimpleActorRuntime();

            Task mainTask = runtime.StartMain(() =>
            {
                // Normal actors.

                // Creates an actor and yields its mailbox.
                // Only the owner of a mailbox can call Receive.
                // All actors can Send.
                // Every actor has its own mailbox that stores objects.
                IMailbox<object> helloActorMailbox =
                    runtime.Create(new HelloActor());

                helloActorMailbox.Send("hello");
                helloActorMailbox.Send("world");

                // Creates another (separate) mailbox for the current actor.
                // The main mailbox of the current actor is not used in this example.
                // Additional mailboxes can have a more specific type parameter than object.
                IMailbox<string> separateMailbox =
                    runtime.CreateMailbox<string>();

                // send it to helloActor
                helloActorMailbox.Send(separateMailbox);
                // receive a string on this separate mailbox (no casting)
                string textResult = separateMailbox.Receive();
                Console.WriteLine(textResult);


                // Typed actors. 

                // Built on top of IActorRuntime
                // (which could be implemented in many ways).
                ITypedActorRuntime typedRuntime =
                    new SimpleTypedActorRuntime(runtime);

                // Create a typed actor: answerPhone is a proxy object.
                // The proxy class for IAnswerPhone is generated dynamically (once)
                // at runtime using System.Reflection.Emit.
                IAnswerPhone answerPhone =
                    typedRuntime.Create<IAnswerPhone>(new AnswerPhone());

                IPhoner phoner1 = typedRuntime.Create<IPhoner>(new Phoner());
                IPhoner phoner2 = typedRuntime.Create<IPhoner>(new Phoner());

                // Send messages to the actors by invoking methods.
                phoner1.Init(1, answerPhone);
                phoner2.Init(2, answerPhone);
                phoner2.Go();
                phoner1.Go();

                // We use a (lower level) additional mailbox to receive results.
                // There are other possible approaches.
                // E.g. methods that have return values could be translated into
                // something that is equivalent to the code below.
                IMailbox<string> returnMailbox = runtime.CreateMailbox<string>();

                answerPhone.CheckMessages(returnMailbox);
                Console.WriteLine("\nAttempt 1:\n\n" + returnMailbox.Receive());

                Thread.Sleep(1000);

                answerPhone.CheckMessages(returnMailbox);
                Console.WriteLine("\nAttempt 2:\n\n" + returnMailbox.Receive());
            });

            mainTask.Wait();



            // Random testing (can use actors or typed actors).

            // The implementation of TestActorRuntime can also be
            // replaced with another implementation 
            // (still need to make an interface for this though). 
            // Or, a different implementation of IActorRuntime
            // can be provided that does testing (using a different
            // test harness).

            ITestLauncher testLauncher = new TestLauncher();
            testLauncher.SetScheduler(new RandomScheduler());

            for (int i = 1; i <= 100; ++i)
            {
                Console.WriteLine("\n\n... ITERATION " + i + "\n\n");
                testLauncher.Execute((runtime2, testingRuntime) =>
                {
                    IMailbox<object> helloActorMailbox =
                        runtime2.Create(new HelloActor());

                    helloActorMailbox.Send("hello");
                    helloActorMailbox.Send("world");

                    IMailbox<string> separateMailbox =
                        runtime2.CreateMailbox<string>();
                    helloActorMailbox.Send(separateMailbox);
                    Console.WriteLine(separateMailbox.Receive());
                });
            }

            Console.WriteLine("[Done]");
            Console.ReadLine();
        }
    }
}
