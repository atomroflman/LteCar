using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using LteCar.Shared.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard.Hardware
{
    /// <summary>
    /// Factory for creating IModuleManager instances based on ChannelMap configuration.
    /// </summary>
    public class ModuleManagerFactory : IModuleManagerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Bash _bash;
        private readonly IServiceProvider _serviceProvider;
        private readonly ChannelMap _channelMap;

        /// <summary>
        /// Constructor. All dependencies are injected via DI.
        /// </summary>
        public ModuleManagerFactory(ILoggerFactory loggerFactory, Bash bash, IServiceProvider serviceProvider, ChannelMap channelMap)
        {
            _loggerFactory = loggerFactory;
            _bash = bash;
            _serviceProvider = serviceProvider;
            _channelMap = channelMap;
        }

        /// <summary>
        /// Creates an IModuleManager instance for the given pin manager name using ChannelMap.
        /// </summary>
        public IModuleManager Create(string name)
        {
            // Find pin manager config by name
            if (!_channelMap.PinManagers.TryGetValue(name, out var managerConfig))
                throw new ArgumentException($"No pinManager named '{name}' in ChannelMap.");

            var typeName = managerConfig.Type;
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException($"No type defined for pinManager '{name}'.");

            // Find all types matching the type name (without namespace)
            var matchingTypes = Assembly.GetExecutingAssembly().GetTypes();
            var foundTypes = new List<Type>();
            foreach (var t in matchingTypes)
            {
                if (t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    foundTypes.Add(t);
            }
            if (foundTypes.Count == 0)
                throw new NotSupportedException($"Type '{typeName}' not found.");
            if (foundTypes.Count > 1)
            {
                _loggerFactory.CreateLogger<ModuleManagerFactory>()
                    .LogWarning($"Multiple types named '{typeName}' found. Using the first: {foundTypes[0].FullName}");
            }
            var type = foundTypes[0];

            // Create instance using DI
            object? instance = null;
            if (type == typeof(Pca9685PwmExtension))
            {
                var logger = _loggerFactory.CreateLogger<Pca9685PwmExtension>();
                instance = ActivatorUtilities.CreateInstance(_serviceProvider, type, logger, _bash);
            }
            else
            {
                instance = ActivatorUtilities.CreateInstance(_serviceProvider, type);
            }

            // Set properties from options dictionary
            foreach (var kv in managerConfig.Options)
            {
                var pi = type.GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null && pi.CanWrite)
                {
                    object? value = null;
                    if (pi.PropertyType == typeof(int) && int.TryParse(kv.Value, out var intVal))
                        value = intVal;
                    else if (pi.PropertyType == typeof(string))
                        value = kv.Value;
                    else if (pi.PropertyType == typeof(bool) && bool.TryParse(kv.Value, out var boolVal))
                        value = boolVal;
                    // Add more type conversions as needed
                    if (value != null)
                        pi.SetValue(instance, value);
                }
            }
            return instance as IModuleManager ?? throw new InvalidCastException($"{typeName} does not implement IModuleManager");
        }
    }
}
