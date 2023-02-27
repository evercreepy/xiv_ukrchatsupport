using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using UkrChatSupportPlugin.Sys;

namespace UkrChatSupportPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class UkrChatSupportPlugin : IDalamudPlugin
{
    private uint foregroundThreadId;
    private IntPtr foregroundWindow;
    private CancellationTokenSource stopToken;

    public UkrChatSupportPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ChatGui chatGui)
    {
        PluginInterface = pluginInterface;
        Chat = chatGui;
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        if (Configuration.ReactOnlyToUkLayout)
        {
            InitCheckerThread();
        }

        Chat.CheckMessageHandled += ChatOnCheckMessageHandled;
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    public Configuration Configuration { get; init; }
    public ChatGui Chat { get; init; }
    public string Name => "UkrChatSupport";

    public void Dispose()
    {
        stopToken.Cancel();
        stopToken.Dispose();
        Chat.CheckMessageHandled -= ChatOnCheckMessageHandled;
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
        XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (isHandled) return;
        if (Configuration.ReactOnlyToUkLayout)
        {
            var currentLayout = NativeMethods.GetCurrentKeyboardLayout(foregroundThreadId);
            // "uk" - ukrainian
            if (!currentLayout.TwoLetterISOLanguageName.Equals("uk")) return;
        }

        ReplaceSymbols(ref message);
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
                             .Replace("Є", "\u2208"); // \u0404 yo \u2208

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
            while (!stopToken.IsCancellationRequested)
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
}
