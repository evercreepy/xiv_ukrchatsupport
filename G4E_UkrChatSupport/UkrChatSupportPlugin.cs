using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using UkrChatSupportPlugin.Sys;
using UkrChatSupportPlugin.Windows;

namespace UkrChatSupportPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class UkrChatSupportPlugin : IDalamudPlugin
{
    public readonly WindowSystem WindowSystem;
    private uint foregroundThreadId;
    private IntPtr foregroundWindow;
    private CancellationTokenSource? stopToken;

    public UkrChatSupportPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ChatGui chatGui)
    {
        PluginInterface = pluginInterface;
        Chat = chatGui;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        WindowSystem = new WindowSystem(typeof(UkrChatSupportPlugin).AssemblyQualifiedName);
        var configWindow = PluginInterface.Create<ConfigWindow>(this);
        ConfigWindow = configWindow ?? new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        if (Configuration.ReactOnlyToUkLayout) InitCheckerThread();

        Chat.CheckMessageHandled += ChatOnCheckMessageHandled;
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    public Configuration Configuration { get; init; }
    public ChatGui Chat { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    public string Name => "G4E UkrChatSupport";

    public void Dispose()
    {
        stopToken?.Cancel();
        stopToken?.Dispose();
        Chat.CheckMessageHandled -= ChatOnCheckMessageHandled;
        GC.SuppressFinalize(this);
    }

    private void InitCheckerThread()
    {
        stopToken = new CancellationTokenSource();
        // Checking foreground window
        var backgroundThread = new Thread(BackgroundWorker)
        {
            IsBackground = true,
            Name = "Get foreground window thread"
        };
        backgroundThread.Start();
    }

    private void ChatOnCheckMessageHandled(
        XivChatType chatType, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (isHandled || !IsChatTypeSupported(chatType)) return;
        if (Configuration.ReactOnlyToUkLayout)
        {
            var currentLayout = NativeMethods.GetCurrentKeyboardLayout(foregroundThreadId);
            // "uk" - ukrainian
            if (!currentLayout.TwoLetterISOLanguageName.Equals("uk")) return;
        }

        ReplaceSymbols(ref message);
    }

    private bool IsChatTypeSupported(XivChatType type)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (type)
        {
            case XivChatType.None:
            case XivChatType.Debug:
            case XivChatType.Urgent:
            case XivChatType.Notice:
            case XivChatType.SystemError:
            case XivChatType.SystemMessage:
            case XivChatType.GatheringSystemMessage:
            case XivChatType.ErrorMessage:
            case XivChatType.NPCDialogue:
            case XivChatType.NPCDialogueAnnouncements:
            case XivChatType.RetainerSale:
                return false;
            default:
                return true;
        }
    }

    private void ReplaceSymbols(ref SeString message)
    {
        try
        {
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (TextPayload textPayload in message.Payloads.Where(p => p is TextPayload))
            {
                var input = textPayload.Text;
                if (string.IsNullOrWhiteSpace(input)) continue;

                var output = input
                             .Replace("і", "i")       // \u0456 to \u0069
                             .Replace("І", "I")       // \u0406 to \u0073
                             .Replace("ї", "ï")       // \u1111 to \u00EF
                             .Replace("Ї", "Ï")       // \u1031 to \u00CF
                             .Replace("є", "\u2208")  // \u0454 to \u2208
                             .Replace("Є", "\u2208"); // \u0404 to \u2208

                textPayload.Text = output;
                PluginLog.LogDebug($"{input}|{output}");
            }
        }
        catch (Exception e)
        {
            PluginLog.LogDebug(e, $"Failed to replace symbols for: {message}");
            PluginLog.LogError(e, e.Message);
        }
    }

    private void GetForeground()
    {
        foregroundWindow = NativeMethods.GetForegroundWindow();
        foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, nint.Zero);
    }

    private void BackgroundWorker()
    {
        try
        {
            GetForeground();
            while (stopToken?.IsCancellationRequested == false)
            {
                Task.Delay(1000, stopToken.Token).Wait();
                GetForeground();
            }
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, e.Message);
        }
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
