
using System;
using System.Threading;
using System.Threading.Tasks;
using ActorFramework;
using ActorInterface;
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
                IActorRuntime runtime = new SimpleActorRuntime();
                ITypedActorRuntime typedRuntime = new SimpleTypedActorRuntime(runtime);

                IAnswerPhone answerPhone =
                    typedRuntime.Create<IAnswerPhone>(new AnswerPhone());

                IPhoner phoner1 = typedRuntime.Create<IPhoner>(new Phoner(1));
                IPhoner phoner2 = typedRuntime.Create<IPhoner>(new Phoner(2));

                phoner1.SetAnswerPhone(answerPhone);
                phoner2.SetAnswerPhone(answerPhone);

                phoner2.Go();
                phoner1.Go();


                var res = runtime.CreateMailbox<string>();

                answerPhone.CheckMessages(res);
                Console.WriteLine(res.Receive());

                Thread.Sleep(500);

                answerPhone.CheckMessages(res);
                Console.WriteLine(res.Receive());

                //                var helloMailbox = runtime.Create(new HelloActor());
                //
                //                helloMailbox.Send("hello");
                //                helloMailbox.Send("world");
                //
                //                var separateMailbox = runtime.CreateMailbox<string>();
                //                helloMailbox.Send(separateMailbox);
                //                var res = separateMailbox.Receive();
                //                Console.WriteLine(res);
            });

            task.Wait();

            Console.ReadLine();
        }
    }
}
