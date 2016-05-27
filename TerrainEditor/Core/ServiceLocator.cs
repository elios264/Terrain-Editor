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
                                  .ToDictionary(type => type.GetCustomAttribute<IsServiceAttribute>().ServiceInterfaceType, 
                                                Activator.CreateInstance);
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