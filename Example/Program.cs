
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
            Console.ReadLine();
            var start = DateTime.Now;

            var task = Task.Factory.StartNew(() =>
            {

                //                IActorRuntime runtime = new SimpleActorRuntime();
                //                ITypedActorRuntime typedRuntime = new SimpleTypedActorRuntime(runtime);
                //
                //
                //                // Normal actors
                //
                //                var helloActorMailbox = runtime.Create(new HelloActor());
                //                
                //                helloActorMailbox.Send("hello");
                //                helloActorMailbox.Send("world");
                //                
                //                var separateMailbox = runtime.CreateMailbox<string>();
                //                helloActorMailbox.Send(separateMailbox);
                //                Console.WriteLine(separateMailbox.Receive());
                //
                //
                //                // Typed actors
                //
                //                IAnswerPhone answerPhone =
                //                    typedRuntime.Create<IAnswerPhone>(new AnswerPhone());
                //
                //                IPhoner phoner1 = typedRuntime.Create<IPhoner>(new Phoner());
                //                IPhoner phoner2 = typedRuntime.Create<IPhoner>(new Phoner());
                //
                //                phoner1.Init(1, answerPhone);
                //                phoner2.Init(2, answerPhone);
                //
                //                phoner2.Go();
                //                phoner1.Go();
                //
                //
                //                var res = runtime.CreateMailbox<string>();
                //
                //                answerPhone.CheckMessages(res);
                //                Console.WriteLine("\nAttempt 1:\n\n" + res.Receive());
                //
                //                Thread.Sleep(1000);
                //
                //                answerPhone.CheckMessages(res);
                //                Console.WriteLine("\nAttempt 2:\n\n" + res.Receive());

                // 3.97 seconds

                

                
            });

            task.Wait();

            ITestingRuntime testingRuntime = new TestingRuntime();
            testingRuntime.SetScheduler(new RandomScheduler());

            for (int i = 1; i <= 100000; ++i)
            {
                Console.WriteLine("\n\n... ITERATION " + i + "\n\n");
                testingRuntime.Execute(runtime =>
                {
                    var helloActorMailbox = runtime.Create(new HelloActor());

                    helloActorMailbox.Send("hello");
                    helloActorMailbox.Send("world");

                    var separateMailbox = runtime.CreateMailbox<string>();
                    helloActorMailbox.Send(separateMailbox);
                    Console.WriteLine(separateMailbox.Receive());
                });
            }

            var time = DateTime.Now - start;
            Console.WriteLine(time.TotalSeconds);


        }
    }
}
