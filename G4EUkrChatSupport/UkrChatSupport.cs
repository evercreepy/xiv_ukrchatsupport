using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using UkrChatSupportPlugin.Sys;
using UkrChatSupportPlugin.Windows;

namespace UkrChatSupportPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class UkrChatSupport : IDalamudPlugin
{
    private readonly List<KeyReplace> replaceKeys = new()
    {
        // "і"
        new KeyReplace
        {
            Key = Forms.Keys.S,
            RKey = 105,
            RCapitalKey = 73
        },
        // "ї"
        new KeyReplace
        {
            Key = Forms.Keys.Oem6,
            RKey = 239,
            RCapitalKey = 207
        },
        // "є"
        new KeyReplace
        {
            Key = Forms.Keys.Oem7,
            RKey = 8712,
            RCapitalKey = 8712
        }
    };

    private uint foregroundThreadId;
    private IntPtr foregroundWindow;
    private KeyboardHook keyboardHook;

    private CancellationTokenSource? stopToken;

    public WindowSystem WindowSystem;

    public UkrChatSupport(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ChatGui chatGui, [RequiredVersion("1.0")] GameGui gameGui,
        [RequiredVersion("1.0")] Framework framework)
    {
        PluginInterface = pluginInterface;
        Chat = chatGui;
        Game = gameGui;

        framework.RunOnFrameworkThread(Setup);
    }

    private DalamudPluginInterface PluginInterface { get; init; }
    public Configuration Configuration { get; set; }
    public ChatGui Chat { get; set; }
    public GameGui Game { get; set; }
    private ConfigWindow ConfigWindow { get; set; }
    public string Name => "G4E UkrChatSupport";

    public void Dispose()
    {
        stopToken?.Cancel();
        stopToken?.Dispose();
        Chat.CheckMessageHandled -= ChatOnCheckMessageHandled;
        keyboardHook.KeyDown -= Handle_keyboardHookOnKeyDown;
        keyboardHook.OnError -= Handle_keyboardHook_OnError;
        keyboardHook.Dispose();
    }

    private void Setup()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        WindowSystem = new WindowSystem(typeof(UkrChatSupport).AssemblyQualifiedName);
        var configWindow = PluginInterface.Create<ConfigWindow>(this);
        ConfigWindow = configWindow ?? new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        if (Configuration.ReactOnlyToUkLayout) InitCheckerThread();

        Chat.CheckMessageHandled += ChatOnCheckMessageHandled;

        keyboardHook = new KeyboardHook(true);
        keyboardHook.KeyDown += Handle_keyboardHookOnKeyDown;
        keyboardHook.OnError += Handle_keyboardHook_OnError;

        WriteCurrentConfig();
    }

    private void WriteCurrentConfig()
    {
        PluginLog.LogInformation($"Configuration.ReactOnlyToUkLayout - {Configuration.ReactOnlyToUkLayout}");
        PluginLog.LogInformation($"Configuration.ReplaceOnlyOnUkLayout - {Configuration.ReplaceOnlyOnUkLayout}");
        PluginLog.LogInformation($"Configuration.ReplaceInput - {Configuration.ReplaceInput}");
    }

    private void Handle_keyboardHookOnKeyDown(Forms.Keys key, bool shift, bool ctrl, bool alt, ref bool skipNext)
    {
        try
        {
            if (!Configuration.ReplaceInput || (Configuration.ReplaceOnlyOnUkLayout && !IsUkrainianLayout())) return;
            if (!IsTyping())
            {
                PluginLog.LogInformation("Not typing, skip!");
                return;
            }

            ReplaceInput(key, shift, ref skipNext);
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, e.Message);
        }
    }

    private void ReplaceInput(Forms.Keys aKey, bool aIsShift, ref bool aNextSkip)
    {
        foreach (var keyReplace in replaceKeys.Where(keyReplace => keyReplace.Key == aKey))
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
        PluginLog.LogInformation($"Current layout - {currentLayout.TwoLetterISOLanguageName}");
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
        ConfigWindow.Toggle();
    }
}
