using Urho;
using Urho.Audio;
using Urho.Gui;
using Urho.IO;
using Urho.Resources;

namespace TerrainEditor.Core.Services
{
    public interface IUrho3DService
    {
        Scene Scene { get; }
        Camera Camera { get; }
        Engine Engine { get; }
        Input Input { get; }
        Renderer Renderer { get; }
        Log Log { get; }
        ResourceCache ResourceCache { get; }
        UI UI { get; }
        Audio Audio { get; }
        Graphics Graphics { get; }
        Context Context { get; }
        UrhoObject GetSubsystem(StringHash type);
    }
}