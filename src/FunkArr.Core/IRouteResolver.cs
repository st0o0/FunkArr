namespace FunkArr.Core;

public interface IRouteResolver
{
    ResolvedRoute Resolve(string channel);
}
