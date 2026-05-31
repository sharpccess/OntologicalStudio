using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.Services;

/// <summary>
/// Helper to run scoped operations against the DI container.
/// EF Core DbContext is registered as Scoped, so each operation needs its own scope.
/// </summary>
public static class ScopedRunner
{
    public static async Task<T> RunAsync<TService, T>(
        IServiceProvider provider,
        Func<TService, Task<T>> action)
        where TService : notnull
    {
        await using var scope = provider.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(svc);
    }

    public static async Task RunAsync<TService>(
        IServiceProvider provider,
        Func<TService, Task> action)
        where TService : notnull
    {
        await using var scope = provider.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<TService>();
        await action(svc);
    }
}
