using System.Runtime.CompilerServices;
using DiffEngine;

namespace FunkArr.Persistence.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => DiffRunner.Disabled = true;
}
