using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace TerrainEditor.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type,object> Services;

        static ServiceLocator()
        {
            var appAssembly = Assembly.GetAssembly(Application.Current.GetType());

            Services = appAssembly.GetTypes()
                                  .Where(type => Attribute.IsDefined(type, typeof(IsServiceAttribute)))
                                  .Select(implementationType =>
                                  {
                                      var serviceInterfaceType = implementationType.GetCustomAttribute<IsServiceAttribute>().ServiceInterfaceType;

                                      if (!serviceInterfaceType.IsAssignableFrom(implementationType))
                                          throw new ArgumentException($"{implementationType.Name} does not implement {serviceInterfaceType.Name}");

                                      return new { serviceInterfaceType, implementationType };
                                  })
                                  .ToDictionary(info => info.serviceInterfaceType, info => Activator.CreateInstance(info.implementationType));
        }

        public static void Register<TServiceInterface>(TServiceInterface instance)
        {
            Services.Add(typeof(TServiceInterface), instance);
        }

        public static T Get<T>()
        {
            return (T) Get(typeof(T));
        }
        public static object Get(Type type)
        {
            return Services[type];
        }
    }
}