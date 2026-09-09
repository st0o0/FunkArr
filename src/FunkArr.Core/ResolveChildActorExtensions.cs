using Akka.Actor;
using Akka.DependencyInjection;

namespace FunkArr.Core;

public static class ResolveChildActorExtensions
{
    public static IActorRef ResolveChildActor<TActor>(
        this IActorContext context,
        string name,
        Func<Props, Props> configure,
        params object[] args) where TActor : ActorBase
    {
        var resolver = DependencyResolver.For(context.System);
        var props = configure(resolver.Props<TActor>(args));
        return context.ActorOf(props, name);
    }
}
