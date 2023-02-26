using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? PluginInterface;

    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public int Version { get; set; } = 0;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}
