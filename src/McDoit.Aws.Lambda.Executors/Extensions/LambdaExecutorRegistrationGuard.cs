using Microsoft.Extensions.DependencyInjection;

namespace McDoit.Aws.Lambda.Executors.Extensions;

public static class LambdaExecutorRegistrationGuard
{
    private sealed record LambdaExecutorRegistrationToken(string RegistrationName);

    public static TResult RegisterExecutor<TResult>(
        IServiceCollection services,
        string registrationName,
        Func<TResult> registrationAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationName);
        ArgumentNullException.ThrowIfNull(registrationAction);

        var existingRegistration = services
            .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(LambdaExecutorRegistrationToken))
            ?.ImplementationInstance as LambdaExecutorRegistrationToken;

        if (existingRegistration is not null)
        {
            throw new InvalidOperationException(
                $"A lambda executor has already been registered using '{existingRegistration.RegistrationName}'. " +
                $"Only one executor registration is supported per service collection. " +
                $"Remove the existing registration before calling '{registrationName}'.");
        }

        var result = registrationAction();
        services.AddSingleton(new LambdaExecutorRegistrationToken(registrationName));
        return result;
    }

    public static void RegisterExecutor(
        IServiceCollection services,
        string registrationName,
        Action registrationAction)
    {
        RegisterExecutor(
            services,
            registrationName,
            () =>
            {
                registrationAction();
                return true;
            });
    }
}
