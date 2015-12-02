using System;
using System.Threading.Tasks;
using ActorInterface;
using TypedActorInterface;

namespace TypedActorFramework
{
    public class SimpleTypedActorRuntime : ITypedActorRuntime
    {
        private readonly IActorRuntime actorRuntime;
        private static readonly ProxyContainer proxies = new ProxyContainer();

        public SimpleTypedActorRuntime(IActorRuntime actorRuntime)
        {
            this.actorRuntime = actorRuntime;
            //            proxies = new ProxyContainer();
        }

        public static void SaveDynamicProxyModule()
        {
            proxies.SaveModule();
        }

        #region Implementation of ITypedActorRuntime

        public T Create<T>(T typedActorInstance, string name = null)
            where T : ITypedActor
        {
            var mailbox =
                actorRuntime.Create<object>(
                    () =>
                    {
                        TypedActorEntryPoint(actorRuntime);
                        return null;
                    },
                    name ?? typeof (T).Name);

            mailbox.Send(typedActorInstance);
            return GetOrCreateProxy<T>(mailbox);
        }

        public T CreateTask<T, TResult>(T typedActorInstance, out Task<TResult> task, string name = null) 
            where T : ITypedActor
        {
            task = actorRuntime.StartNew(() =>
            {
                TypedActorEntryPoint(actorRuntime);
                return default(TResult);
            });

            var mailbox =
                actorRuntime.MailboxFromTask(task);
            mailbox.Send(typedActorInstance);
            return GetOrCreateProxy<T>(mailbox);
        }

        public T ProxyFromMailbox<T>(IMailbox<object> mailbox)
            where T : ITypedActor
        {
            return GetOrCreateProxy<T>(mailbox);
        }

        public void ReceiveCall<T>(IMailbox<object> mailbox, T actorInstance)
            where T : ITypedActor
        {
            var m = (ICallable) mailbox.Receive();
            m.Call(actorInstance);
        }

        #endregion

        private T GetOrCreateProxy<T>(IMailbox<object> mailbox)
            where T : ITypedActor
        {
            Type proxyType = proxies.GetProxyType(typeof(T));
            var res = (T)Activator.CreateInstance(
                proxyType,
                mailbox,
                actorRuntime);
            return res;
        }

        public static object TypedActorEntryPoint(IActorRuntime runtime)
        {
            var mailbox = runtime.CurrentMailbox();

            ITypedActor typedActor = (ITypedActor) mailbox.Receive();

            while (true)
            {
                var msg = (ICallable) mailbox.Receive();
                msg.Call(typedActor);
            }

        }

    }
}