using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace UkrChatSupportPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public delegate void ConfigChanged(Configuration configuration);

    [NonSerialized]
    // ReSharper disable once InconsistentNaming
    private IDalamudPluginInterface? PluginInterface;

    public bool ReactOnlyToUkLayout { get; set; }
    public bool ReplaceOnlyOnUkLayout { get; set; } = true;
    public bool ReplaceInput { get; set; } = true;

    public int Version { get; set; } = 1;

    public event ConfigChanged? OnConfigChanged;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
        OnConfigChanged?.Invoke(this);
    }
}
