using System.Diagnostics;
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
        Size = new Vector2(320, 145);
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

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);
        if (ImGui.Button("Buy Me a Coffee"))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.buymeacoffee.com/justscribe",
                UseShellExecute = true,
            });
        }

        ImGui.SameLine();
        if (ImGui.Button("Gaming For Eternity"))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.gaming4eternity.online",
                UseShellExecute = true,
            });
        }
    }
}
