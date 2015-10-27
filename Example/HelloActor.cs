using System;
using ActorInterface;

namespace Example
{
    public class HelloActor : IEntryPoint
    {
        #region Implementation of IEntryPoint

        public void EntryPoint(IActorRuntime runtime)
        {
            var myMailbox = runtime.CurrentMailbox();

            object msg = myMailbox.Receive();

            Console.WriteLine(msg);

            msg = myMailbox.Receive();

            Console.WriteLine(msg);

            IMailbox<string> mailbox = (IMailbox<string>) myMailbox.Receive();

            mailbox.Send("message for separate mailbox");

            Console.WriteLine(
                $"I am actor with mailbox {myMailbox} and I am about to terminate.");
        }

        #endregion
    }
}