using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using UkrChatSupportPlugin.Sys;
using UkrChatSupportPlugin.Windows;

namespace UkrChatSupportPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class UkrChatSupport : IDalamudPlugin
{
    private int currentThreadId;
    private uint foregroundThreadId;
    private IntPtr foregroundWindow;
    private bool isDisposed;
    private volatile bool isHooked;
    private KeyboardHook? keyboardHook;

    private CancellationTokenSource? stopToken;

#pragma warning disable CS8618
    /// <summary>
    ///     Plugin setup in Framework thread
    /// </summary>
    /// <param name="pluginInterface"></param>
    /// <param name="chatGui"></param>
    /// <param name="gameGui"></param>
    /// <param name="framework"></param>
    public UkrChatSupport(
        DalamudPluginInterface pluginInterface,
        ChatGui chatGui, GameGui gameGui,
        Framework framework)
    {
        PluginInterface = pluginInterface;
        Framework = framework;
        Chat = chatGui;
        Game = gameGui;

        Framework.RunOnFrameworkThread(Setup);
    }
#pragma warning restore CS8618

    private DalamudPluginInterface PluginInterface { get; init; }
    private Framework Framework { get; init; }
    public WindowSystem WindowSystem { get; set; }
    public Configuration Configuration { get; set; }
    public ChatGui Chat { get; set; }
    public GameGui Game { get; set; }
    private ConfigWindow ConfigWindow { get; set; }
    public string Name => "G4E UkrChatSupport";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool isDisposing)
    {
        if (!isDisposed)
        {
            if (isDisposing)
            {
                stopToken?.Cancel();
                stopToken?.Dispose();
                Chat.CheckMessageHandled -= ChatOnCheckMessageHandled;
                StopHook();
            }

            stopToken = null;
            keyboardHook = null;
            isDisposed = true;
        }
    }

    private void Setup()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        Configuration.OnConfigChanged += Handle_ConfigurationOnOnConfigChanged;

        WindowSystem = new WindowSystem(typeof(UkrChatSupport).AssemblyQualifiedName);
        var configWindow = PluginInterface.Create<ConfigWindow>(this);
        ConfigWindow = configWindow ?? new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        if (Configuration.ReactOnlyToUkLayout || Configuration.ReplaceOnlyOnUkLayout) InitCheckerThread();

        Chat.CheckMessageHandled += ChatOnCheckMessageHandled;

        currentThreadId = KeyboardHook.GetCurrentThreadId();
        StartHook();
    }

    private void Handle_ConfigurationOnOnConfigChanged(Configuration configuration)
    {
#if DEBUG
        WriteCurrentConfig();
#endif
        if (keyboardHook == null) return;
        if (configuration.ReplaceInput)
        {
            keyboardHook.KeyDown -= Handle_keyboardHookOnKeyDown;
            keyboardHook.KeyDown += Handle_keyboardHookOnKeyDown;
        }
        else
            keyboardHook.KeyDown -= Handle_keyboardHookOnKeyDown;
    }

    private void WriteCurrentConfig()
    {
        PluginLog.LogInformation($"Configuration.ReactOnlyToUkLayout - {Configuration.ReactOnlyToUkLayout}");
        PluginLog.LogInformation($"Configuration.ReplaceOnlyOnUkLayout - {Configuration.ReplaceOnlyOnUkLayout}");
        PluginLog.LogInformation($"Configuration.ReplaceInput - {Configuration.ReplaceInput}");
    }

    private void Handle_keyboardHookOnKeyDown(Constants.Keys key, bool shift, bool ctrl, bool alt, ref bool skipNext)
    {
        try
        {
            if (!IsInsideFFXIV() ||
                !Configuration.ReplaceInput ||
                !IsTyping() ||
                (Configuration.ReplaceOnlyOnUkLayout && !IsUkrainianLayout())) return;

            ReplaceInput(key, shift, ref skipNext);
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, e.Message);
        }
    }

    private void ReplaceInput(Constants.Keys aKey, bool aIsShift, ref bool aNextSkip)
    {
        foreach (var keyReplace in Constants.ReplaceKeys.Where(keyReplace => keyReplace.Key == aKey))
        {
            aNextSkip = true;
            KeyboardHook.SendCharUnicode(aIsShift ? keyReplace.RCapitalKey : keyReplace.RKey);
            return;
        }
    }

    private void Handle_keyboardHook_OnError(Exception e)
    {
        PluginLog.LogError(e, e.Message);
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
        if (Configuration.ReactOnlyToUkLayout && !IsUkrainianLayout()) return;

        ReplaceSymbols(ref message);
    }

    private bool IsUkrainianLayout()
    {
        var currentLayout = NativeMethods.GetCurrentKeyboardLayout(foregroundThreadId);
        // "uk" - ukrainian
        return currentLayout.TwoLetterISOLanguageName.Equals("uk");
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

    private bool IsInsideFFXIV()
    {
        return foregroundThreadId.Equals((uint)currentThreadId);
    }

    private void StartHook()
    {
        Framework.RunOnFrameworkThread(() =>
        {
            if (isHooked || keyboardHook is not null) return;
            try
            {
                keyboardHook = new KeyboardHook(true);
                keyboardHook.KeyDown += Handle_keyboardHookOnKeyDown;
                keyboardHook.OnError += Handle_keyboardHook_OnError;
                isHooked = true;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
            }
        });
    }

    private void StopHook()
    {
        Framework.RunOnFrameworkThread(() =>
        {
            if (!isHooked || keyboardHook is null) return;
            try
            {
                keyboardHook.KeyDown -= Handle_keyboardHookOnKeyDown;
                keyboardHook.OnError -= Handle_keyboardHook_OnError;
                keyboardHook.Dispose();
                keyboardHook = null;
                isHooked = false;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
            }
        });
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
            }
        }
        catch (Exception e)
        {
            PluginLog.LogDebug(e, $"Failed to replace symbols for: {message}");
            PluginLog.LogError(e, e.Message);
        }
    }

    private unsafe bool IsTyping()
    {
        var chatLog = (AtkUnitBase*)Game.GetAddonByName("ChatLog");

        if (!chatLog->IsVisible) return false;

        var textInput = chatLog->UldManager.NodeList[15];
        var chatCursor = textInput->GetAsAtkComponentNode()->Component->UldManager.NodeList[14];

        return chatCursor->IsVisible;
    }

    private void GetForeground()
    {
        foregroundWindow = NativeMethods.GetForegroundWindow();
        foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, nint.Zero);
        if (IsInsideFFXIV())
            StartHook();
        else
            StopHook();
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
        catch (TaskCanceledException) { }
        catch (AggregateException ae)
        {
            if (ae.InnerException is not TaskCanceledException) throw;
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
        ConfigWindow.Toggle();
    }

    ~UkrChatSupport()
    {
        Dispose(false);
    }
}
