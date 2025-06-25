using FS.AspNetCore.ResponseWrapper.Filters;
using FS.AspNetCore.ResponseWrapper.Middlewares;
using FS.AspNetCore.ResponseWrapper.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FS.AspNetCore.ResponseWrapper;

/// <summary>
/// Provides extension methods for configuring ResponseWrapper services in the dependency injection container.
/// This class implements the Service Registration Pattern, allowing developers to easily integrate
/// ResponseWrapper functionality into their ASP.NET Core applications with flexible configuration options.
/// </summary>
/// <remarks>
/// The registration methods follow a progressive disclosure approach:
/// 1. Simple registration with defaults for quick setup
/// 2. Configuration-based registration for customization
/// 3. Full control registration for advanced scenarios
/// 
/// This design pattern allows developers to start simple and gradually add complexity as needed,
/// following the principle of "convention over configuration" while still providing full customization
/// when required. The registration process handles all internal dependencies automatically,
/// ensuring proper object lifecycle management and preventing common DI configuration mistakes.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Adds ResponseWrapper services to the dependency injection container with default configuration.
    /// This is the simplest way to integrate ResponseWrapper into your application and provides
    /// sensible defaults that work well for most scenarios.
    /// </summary>
    /// <param name="services">The service collection to add ResponseWrapper services to</param>
    /// <returns>The service collection to enable method chaining in startup configuration</returns>
    /// <remarks>
    /// This overload configures ResponseWrapper with these default settings:
    /// - Execution time tracking: Enabled
    /// - Pagination metadata: Enabled  
    /// - Correlation ID tracking: Enabled
    /// - Query statistics: Disabled (requires additional setup)
    /// - DateTime provider: UTC DateTime
    /// - Response wrapping: Enabled for both success and error responses
    /// 
    /// The method automatically registers the filter globally, meaning all API controllers
    /// will have their responses wrapped without requiring additional attributes or configuration.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// services.AddResponseWrapper();
    /// </code>
    /// </example>
    public static IServiceCollection AddResponseWrapper(this IServiceCollection services)
    {
        return services.AddResponseWrapper(options => { });
    }

    /// <summary>
    /// Adds ResponseWrapper services to the dependency injection container with custom configuration options.
    /// This method provides fine-grained control over ResponseWrapper behavior while maintaining
    /// automatic dependency resolution and lifecycle management.
    /// </summary>
    /// <param name="services">The service collection to add ResponseWrapper services to</param>
    /// <param name="configureOptions">
    /// Action delegate that receives a ResponseWrapperOptions instance for configuration.
    /// This allows you to customize behavior such as enabling query statistics,
    /// excluding specific paths, or disabling certain metadata features.
    /// </param>
    /// <returns>The service collection to enable method chaining in startup configuration</returns>
    /// <remarks>
    /// This method follows the Options Pattern recommended by Microsoft for .NET applications.
    /// The configuration action is executed during service registration, and the resulting
    /// options object is registered as a singleton for consistent behavior across requests.
    /// 
    /// The registration process ensures proper dependency order:
    /// 1. Options are configured and registered first
    /// 2. DateTime provider is resolved or defaulted
    /// 3. Filter and middleware are registered with all dependencies
    /// 4. Global filter registration is configured
    /// 
    /// This approach prevents circular dependencies and ensures all services have their
    /// dependencies available when instantiated.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddResponseWrapper(options =>
    /// {
    ///     options.EnableQueryStatistics = true;
    ///     options.ExcludedPaths = new[] { "/health", "/metrics" };
    ///     options.EnableExecutionTimeTracking = false;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddResponseWrapper(
        this IServiceCollection services, 
        Action<ResponseWrapperOptions> configureOptions)
    {
        // Validate input parameters to provide clear error messages
        if (services == null)
            throw new ArgumentNullException(nameof(services), "Service collection cannot be null");
        
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions), "Configuration action cannot be null");

        try
        {
            // Create and configure options using the provided configuration action
            var options = new ResponseWrapperOptions();
            configureOptions(options);

            // Register the configured options as a singleton for consistent access across the application
            // Singleton lifetime ensures all components see the same configuration throughout the app lifecycle
            services.AddSingleton(options);

            // Configure DateTime provider - use custom one if provided, otherwise default to UTC
            // This pattern allows for dependency injection of time providers, which is crucial for testing
            RegisterDateTimeProvider(services, options);

            // Register core ResponseWrapper components with proper dependency injection
            RegisterCoreComponents(services);

            // Configure MVC to automatically apply the response wrapper filter to all API controllers
            ConfigureMvcIntegration(services);

            return services;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to register ResponseWrapper services. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Adds ResponseWrapper services with custom logger type and DateTime provider for advanced scenarios.
    /// This overload provides maximum control over dependencies, allowing you to inject custom
    /// implementations for logging and time handling while maintaining automatic service resolution.
    /// </summary>
    /// <typeparam name="TLogger">
    /// Custom logger type that implements ILogger&lt;ApiResponseWrapperFilter&gt;.
    /// This allows you to provide specialized logging implementations for ResponseWrapper operations,
    /// such as structured logging to specific sinks or custom log formatting.
    /// </typeparam>
    /// <param name="services">The service collection to add ResponseWrapper services to</param>
    /// <param name="dateTimeProvider">
    /// Custom function to provide current DateTime. This is particularly useful for:
    /// - Testing scenarios where you need deterministic time values
    /// - Applications requiring specific timezone handling
    /// - Scenarios where you want to mock time for integration tests
    /// </param>
    /// <param name="configureOptions">
    /// Optional action to configure additional ResponseWrapper options.
    /// If not provided, default options will be used for all other settings.
    /// </param>
    /// <returns>The service collection to enable method chaining in startup configuration</returns>
    /// <remarks>
    /// This advanced registration method is designed for scenarios where you need fine-grained
    /// control over dependencies. Common use cases include:
    /// 
    /// 1. Testing environments where you need to control time and logging
    /// 2. Multi-tenant applications with tenant-specific logging requirements  
    /// 3. Applications with specific timezone or time source requirements
    /// 4. Integration with custom observability platforms
    /// 
    /// The method ensures that your custom logger is registered appropriately in the DI container
    /// and that the DateTime provider is used consistently across all ResponseWrapper components.
    /// It maintains the same automatic filter registration as other overloads.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddResponseWrapper&lt;CustomApiLogger&gt;(
    ///     () => DateTime.Now, // Use local time instead of UTC
    ///     options =>
    ///     {
    ///         options.EnableExecutionTimeTracking = true;
    ///         options.ExcludedPaths = new[] { "/internal" };
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddResponseWrapper<TLogger>(
        this IServiceCollection services,
        Func<DateTime> dateTimeProvider,
        Action<ResponseWrapperOptions>? configureOptions = null)
        where TLogger : class, ILogger<ApiResponseWrapperFilter>
    {
        // Validate input parameters with descriptive error messages
        if (services == null)
            throw new ArgumentNullException(nameof(services), "Service collection cannot be null");
        
        if (dateTimeProvider == null)
            throw new ArgumentNullException(nameof(dateTimeProvider), "DateTime provider function cannot be null");

        try
        {
            // Create and configure options, applying custom configuration if provided
            var options = new ResponseWrapperOptions();
            configureOptions?.Invoke(options);

            // Override DateTime provider with the explicitly provided one
            options.DateTimeProvider = dateTimeProvider;

            // Register configured options and custom DateTime provider
            services.AddSingleton(options);
            services.AddSingleton(dateTimeProvider);

            // Register custom logger if it's not already present in the container
            // This prevents duplicate registrations while allowing override scenarios
            RegisterCustomLogger<TLogger>(services);

            // Register core components with custom dependencies
            RegisterCoreComponents(services);

            // Configure MVC integration
            ConfigureMvcIntegration(services);

            return services;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to register ResponseWrapper services with custom logger {typeof(TLogger).Name}. " +
                "See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Configures the DateTime provider registration based on options configuration.
    /// This method implements the Strategy Pattern for DateTime provision, allowing
    /// flexible time source configuration while maintaining consistent interfaces.
    /// </summary>
    /// <param name="services">The service collection to register the provider in</param>
    /// <param name="options">The options containing the potential custom DateTime provider</param>
    private static void RegisterDateTimeProvider(IServiceCollection services, ResponseWrapperOptions options)
    {
        if (options.DateTimeProvider != null)
        {
            // Use the custom DateTime provider specified in options
            services.AddSingleton(options.DateTimeProvider);
        }
        else
        {
            // Register default UTC DateTime provider for consistent time handling across applications
            // UTC is preferred for APIs as it avoids timezone-related issues in distributed systems
            services.AddSingleton<Func<DateTime>>(() => DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Registers core ResponseWrapper components with proper dependency injection configuration.
    /// This method handles the complex object graph construction required for ResponseWrapper
    /// functionality while maintaining clean separation of concerns.
    /// </summary>
    /// <param name="services">The service collection to register components in</param>
    /// <remarks>
    /// The registration uses factory patterns to ensure proper dependency resolution order.
    /// Each component is registered as Scoped to align with ASP.NET Core's request lifecycle,
    /// ensuring that components are instantiated per request and properly disposed.
    /// 
    /// The factory approach allows us to resolve dependencies at runtime rather than
    /// registration time, which is crucial for maintaining proper service lifetimes
    /// and avoiding captive dependencies.
    /// </remarks>
    private static void RegisterCoreComponents(IServiceCollection services)
    {
        // Register ApiResponseWrapperFilter with factory pattern for complex dependency resolution
        services.AddScoped<ApiResponseWrapperFilter>(serviceProvider =>
        {
            try
            {
                // Resolve all required dependencies from the service provider
                var logger = serviceProvider.GetRequiredService<ILogger<ApiResponseWrapperFilter>>();
                var dateTimeFunc = serviceProvider.GetRequiredService<Func<DateTime>>();
                var options = serviceProvider.GetRequiredService<ResponseWrapperOptions>();

                // Create filter instance with resolved dependencies
                return new ApiResponseWrapperFilter(logger, dateTimeFunc, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to create ApiResponseWrapperFilter instance. " +
                    "Ensure all required dependencies are properly registered.", ex);
            }
        });

        // Register GlobalExceptionHandlingMiddleware with similar factory pattern
        services.AddScoped<GlobalExceptionHandlingMiddleware>(serviceProvider =>
        {
            try
            {
                // Resolve dependencies for middleware construction
                var logger = serviceProvider.GetRequiredService<ILogger<GlobalExceptionHandlingMiddleware>>();
                var dateTimeFunc = serviceProvider.GetRequiredService<Func<DateTime>>();
                var options = serviceProvider.GetRequiredService<ResponseWrapperOptions>();

                // Create middleware instance with resolved dependencies
                return new GlobalExceptionHandlingMiddleware(logger, dateTimeFunc, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to create GlobalExceptionHandlingMiddleware instance. " +
                    "Ensure all required dependencies are properly registered.", ex);
            }
        });
    }

    /// <summary>
    /// Registers a custom logger implementation if it's not already present in the service container.
    /// This method prevents duplicate registrations while allowing for custom logger injection
    /// in advanced scenarios.
    /// </summary>
    /// <typeparam name="TLogger">The custom logger type to register</typeparam>
    /// <param name="services">The service collection to check and potentially register in</param>
    /// <remarks>
    /// This method implements a defensive registration pattern, checking for existing
    /// registrations before adding new ones. This prevents issues in complex applications
    /// where the same logger might be registered by multiple components or configuration paths.
    /// 
    /// The check is performed by examining the service descriptors for the specific logger interface,
    /// ensuring that we don't inadvertently override existing logging configurations.
    /// </remarks>
    private static void RegisterCustomLogger<TLogger>(IServiceCollection services)
        where TLogger : class, ILogger<ApiResponseWrapperFilter>
    {
        var existingLogger = services.FirstOrDefault(x => 
            x.ServiceType == typeof(ILogger<ApiResponseWrapperFilter>));

        if (existingLogger == null)
        {
            // Register the custom logger as singleton to maintain consistent logging behavior
            // Singleton lifetime is appropriate for loggers as they're typically stateless
            services.AddSingleton<ILogger<ApiResponseWrapperFilter>, TLogger>();
        }
    }

    /// <summary>
    /// Configures MVC options to automatically apply the ApiResponseWrapperFilter to all API controllers.
    /// This method implements the Global Filter Pattern, ensuring consistent response formatting
    /// across the entire application without requiring manual filter application.
    /// </summary>
    /// <param name="services">The service collection containing MVC services</param>
    /// <remarks>
    /// The global filter registration uses the generic AddFilterType method which integrates
    /// properly with the dependency injection system. This ensures that the filter receives
    /// its dependencies from the DI container rather than requiring manual instantiation.
    /// 
    /// The filter is added to the MVC pipeline where it will automatically wrap responses
    /// from controllers marked with the [ApiController] attribute, providing consistent
    /// API response formatting without requiring developers to manually apply attributes
    /// to each controller or action.
    /// </remarks>
    private static void ConfigureMvcIntegration(IServiceCollection services)
    {
        services.Configure<MvcOptions>(mvcOptions =>
        {
            // Add the response wrapper filter to the global filter collection
            // This ensures all API controller responses are automatically wrapped
            mvcOptions.Filters.Add<ApiResponseWrapperFilter>();
        });
    }
}