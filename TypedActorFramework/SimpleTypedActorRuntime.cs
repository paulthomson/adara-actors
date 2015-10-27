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

        public T Create<T>(T typedActorInstance) where T : ITypedActor
        {
            var mailbox =
                actorRuntime.Create(new TypeActorActor());
            mailbox.Send(typedActorInstance);

            Type proxyType = proxies.GetProxyType(typeof(T));
            var res = (T) Activator.CreateInstance(
                proxyType,
                mailbox);

            return res;
        }

        #endregion

    }
}