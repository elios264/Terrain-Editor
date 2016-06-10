using System;

namespace TerrainEditor.Core.Services
{
    public class IsServiceAttribute : Attribute
    {
        public Type ServiceInterfaceType { get; }

        public IsServiceAttribute(Type serviceInterfaceType)
        {
            if (!serviceInterfaceType.IsInterface)
                throw new ArgumentException(nameof(serviceInterfaceType));

            ServiceInterfaceType = serviceInterfaceType;
        }
    }
}