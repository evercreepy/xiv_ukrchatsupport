using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace UkrChatSupportPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // the below exist just to make saving less cumbersome
    [NonSerialized]
    // ReSharper disable once InconsistentNaming
    private DalamudPluginInterface? PluginInterface;

    public int Version { get; set; } = 1;
    public bool ReactOnlyToUkLayout { get; set; } = true;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}
