using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace UkrChatSupportPlugin.Windows;

public class ConfigWindow : Window
{
    // ReSharper disable once InconsistentNaming
    private readonly Configuration Configuration;

    public ConfigWindow(UkrChatSupport plugin) : base("UkrChatSupport Configuration",
                                                      ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                                      ImGuiWindowFlags.NoScrollbar |
                                                      ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(232, 75);
        SizeCondition = ImGuiCond.Always;
        Configuration = plugin.Configuration;
    }

    public override void Draw()
    {
        var configValue = Configuration.ReactOnlyToUkLayout;
        if (ImGui.Checkbox("React only to ukrainian layout", ref configValue))
        {
            Configuration.ReactOnlyToUkLayout = configValue;
            Configuration.Save();
        }
    }
}
