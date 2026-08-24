using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Hosting;

namespace FunkArr.Search;

public abstract class SearchActorBase : ReceiveActor
{
    protected readonly IReadOnlyActorRegistry Registry;
    protected readonly ILoggingAdapter Log;

    protected SearchActorBase(IReadOnlyActorRegistry registry, TimeSpan passivationTimeout)
    {
        Registry = registry;
        Log = Context.GetLogger();
        Context.SetReceiveTimeout(passivationTimeout);
        Receive<ReceiveTimeout>(_ => Passivate());
    }

    private void Passivate()
    {
        Log.Debug("{ActorType} passivating due to inactivity", GetType().Name);
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }
}
