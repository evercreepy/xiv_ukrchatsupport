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
        Size = new Vector2(292, 115);
        SizeCondition = ImGuiCond.Always;
        Configuration = plugin.Configuration;
    }

    public override void Draw()
    {
        var reactOnlyToUkLayout = Configuration.ReactOnlyToUkLayout;
        if (ImGui.Checkbox("React only to ukrainian layout (chat window)", ref reactOnlyToUkLayout))
        {
            Configuration.ReactOnlyToUkLayout = reactOnlyToUkLayout;
            Configuration.Save();
        }

        var replaceOnlyOnUkLayout = Configuration.ReplaceOnlyOnUkLayout;
        if (ImGui.Checkbox("Replace only on ukrainian layout (input field)", ref replaceOnlyOnUkLayout))
        {
            Configuration.ReplaceOnlyOnUkLayout = replaceOnlyOnUkLayout;
            Configuration.Save();
        }

        var replaceInput = Configuration.ReplaceInput;
        if (ImGui.Checkbox("Replace keyboard input", ref replaceInput))
        {
            Configuration.ReplaceInput = replaceInput;
            Configuration.Save();
        }
    }
}
