using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WebCommandLine.TagHelpers;

namespace WebCommandLine
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers commands from the specified assemblies
        /// </summary>
        /// <param name="commandAssemblyMarkerTypes"></param>
        /// <param name="assemblies">Assemblies to scan</param>        
        /// <returns>Service collection</returns>
        public static IServiceCollection AddWebCommandLine(this IServiceCollection services, params Type[] commandAssemblyMarkerTypes)
          => services.AddWebCommandLine(commandAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly), configuration: null);

        /// <summary>
        /// Registers commands from the specified assemblies
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>        
        /// <returns>Service collection</returns>
        public static IServiceCollection AddWebCommandLine(this IServiceCollection services, params Assembly[] assemblies)
            => services.AddWebCommandLine(assemblies, configuration: null);

        /// <summary>
        /// Registers command from the assemblies that contain the specified types
        /// </summary>
        /// <param name="services"></param>
        /// <param name="commandAssemblyMarkerType"></param>
        /// <param name="configuration">The action used to configure the options</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddWebCommandLine(this IServiceCollection services, Type commandAssemblyMarkerType, Action<WebCommandLineConfiguration>? configuration)
            => services.AddWebCommandLine(configuration, commandAssemblyMarkerType.GetTypeInfo().Assembly);


        /// <summary>
        /// Registers command from the assemblies that contain the specified types
        /// </summary>
        /// <param name="services"></param>
        /// <param name="commandAssemblyMarkerTypes"></param>
        /// <param name="configuration">The action used to configure the options</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddWebCommandLine(this IServiceCollection services, IEnumerable<Type> commandAssemblyMarkerTypes, Action<WebCommandLineConfiguration>? configuration)
            => services.AddWebCommandLine(commandAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly), configuration);

        /// <summary>
        /// Registers commands from the specified assemblies
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <param name="configuration">The action used to configure the options</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddWebCommandLine(this IServiceCollection services, Action<WebCommandLineConfiguration>? configuration, params Assembly[] assemblies)
            => services.AddWebCommandLine(assemblies, configuration);

        /// <summary>
        /// Registers commands from the specified assemblies
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <param name="configuration">The action used to configure the options</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddWebCommandLine(this IServiceCollection services, IEnumerable<Assembly> assemblies, Action<WebCommandLineConfiguration>? configuration)
        {
            if (!assemblies.Any())
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            var serviceConfig = new WebCommandLineConfiguration();

            configuration?.Invoke(serviceConfig);

            services.AddTransient<ITagHelperComponent, WebCmdTagHelperComponent>();

            services.AddSingleton(_ => serviceConfig);

            assemblies = assemblies.Distinct().ToArray();

            var interfaceTypes = new[] { typeof(IConsoleCommand) };

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetExportedTypes().Where(a => !a.IsAbstract))
                {
                    if (!interfaceTypes.Any(t => t.IsAssignableFrom(type))) continue;

                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        if (!interfaceTypes.Any(t => t == @interface)) continue;

                        services.AddTransient(@interface, type);
                    }
                }
            }

            return services;
        }
    }
}