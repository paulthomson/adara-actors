
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
            var task = Task.Factory.StartNew(() =>
            {
//                ITestingActorRuntime testingRuntime = new TestingActorRuntime();
//
//                for (int i = 0; i < 300; ++i)
//                {
//                    try
//                    {
//                        testingRuntime.PrepareForNextSchedule();
//                        Console.WriteLine("\n\n... ITERATION " + i + "\n\n");
//
//                        var helloActorMailbox = testingRuntime.Create(new HelloActor());
//
//                        helloActorMailbox.Send("hello");
//                        helloActorMailbox.Send("world");
//
//                        var separateMailbox = testingRuntime.CreateMailbox<string>();
//                        helloActorMailbox.Send(separateMailbox);
//                        Console.WriteLine(separateMailbox.Receive());
//
//                        testingRuntime.Wait();
//
//                    }
//                    catch (ActorTerminatedException)
//                    {
//                    }
//
//                }



                IActorRuntime runtime = new SimpleActorRuntime();
                ITypedActorRuntime typedRuntime = new SimpleTypedActorRuntime(runtime);


                // Normal actors

                var helloActorMailbox = runtime.Create(new HelloActor());
                
                helloActorMailbox.Send("hello");
                helloActorMailbox.Send("world");
                
                var separateMailbox = runtime.CreateMailbox<string>();
                helloActorMailbox.Send(separateMailbox);
                Console.WriteLine(separateMailbox.Receive());


                // Typed actors

                IAnswerPhone answerPhone =
                    typedRuntime.Create<IAnswerPhone>(new AnswerPhone());

                IPhoner phoner1 = typedRuntime.Create<IPhoner>(new Phoner());
                IPhoner phoner2 = typedRuntime.Create<IPhoner>(new Phoner());

                phoner1.Init(1, answerPhone);
                phoner2.Init(2, answerPhone);

                phoner2.Go();
                phoner1.Go();


                var res = runtime.CreateMailbox<string>();

                answerPhone.CheckMessages(res);
                Console.WriteLine("\nAttempt 1:\n\n" + res.Receive());

                Thread.Sleep(1000);

                answerPhone.CheckMessages(res);
                Console.WriteLine("\nAttempt 2:\n\n" + res.Receive());
                
            });

            task.Wait();

            Console.ReadLine();
        }
    }
}
