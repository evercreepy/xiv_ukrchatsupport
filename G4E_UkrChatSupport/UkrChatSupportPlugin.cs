using System.IO;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using SamplePlugin.Windows;

namespace SamplePlugin;

public class UkrChatSupportPlugin : IDalamudPlugin
{
    private const string CommandName = "/g4eukrchat";
    public WindowSystem WindowSystem = new("G4E_UkrChatSupport");

    public UkrChatSupportPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager, [RequiredVersion("1.0")] ChatGui chatGui)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        Chat = chatGui;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        // you might normally want to embed resources and load them from the manifest stream
        var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

        Chat.CheckMessageHandled += ChatOnCheckMessageHandled;

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImage);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });


        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public ChatGui Chat { get; init; }
    public string Name => "Sample Plugin";

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void ChatOnCheckMessageHandled(
        XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        //throw new System.NotImplementedException();
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just display our main ui
        MainWindow.IsOpen = true;
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.IsOpen = true;
    }
}
