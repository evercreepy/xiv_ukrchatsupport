using System;

namespace UkrChatSupportPlugin.Sys;

[Serializable]
internal class KeyReplace
{
    /// <summary>
    ///     Keyboard key
    /// </summary>
    public Forms.Keys Key { get; set; }

    /// <summary>
    ///     UTF Char replacement
    /// </summary>
    public int RKey { get; set; }

    /// <summary>
    ///     UTF Char replacement for capital (Shift pressed)
    /// </summary>
    public int RCapitalKey { get; set; }
}
