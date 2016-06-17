using System.Collections.Generic;
using System.IO;

namespace TerrainEditor.Core.Services
{
    public interface IResourceProviderService
    {
        string WorkPath { get; set; }
        object LoadResource(FileInfo info);
        FileInfo InfoForResource(object resource);
        IEnumerable<object> LoadedResources { get; }
    }
}