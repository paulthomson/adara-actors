using System;
using ActorInterface;
using TypedActorInterface;

namespace TypedActorFramework
{
    public class SimpleTypedActorRuntime : ITypedActorRuntime
    {
        private readonly IActorRuntime actorRuntime;
        private readonly ProxyContainer proxies;

        public SimpleTypedActorRuntime(IActorRuntime actorRuntime)
        {
            this.actorRuntime = actorRuntime;
            proxies = new ProxyContainer();
        }

        #region Implementation of ITypedActorRuntime

        public T Create<T>(T typedActorInstance, string name = null)
            where T : ITypedActor
        {
            var mailbox =
                actorRuntime.Create(
                    new TypeActorActor(),
                    name ?? typeof (T).Name);
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
                mailbox);
            return res;
        }

    }
}