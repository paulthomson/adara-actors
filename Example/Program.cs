
using System;
using System.Threading.Tasks;
using ActorFramework;
using ActorInterface;

namespace Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var task = Task.Factory.StartNew(() =>
            {
                IActorRuntime runtime = new SimpleActorRuntime();

                var helloMailbox = runtime.Create(new HelloActor());

                helloMailbox.Send("hello");
                helloMailbox.Send("world");

                var separateMailbox = runtime.CreateMailbox<string>();
                helloMailbox.Send(separateMailbox);
                var res = separateMailbox.Receive();
                Console.WriteLine(res);
            });

            task.Wait();

            Console.ReadLine();
        }
    }
}
