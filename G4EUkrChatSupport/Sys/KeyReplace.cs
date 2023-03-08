namespace UkrChatSupportPlugin.Sys;

public class KeyReplace
{
    /// <summary>
    ///     Keyboard key
    /// </summary>
    public Constants.Keys Key { get; init; }

    /// <summary>
    ///     UTF Char replacement
    /// </summary>
    public int RKey { get; init; }

    /// <summary>
    ///     UTF Char replacement for capital (Shift pressed)
    /// </summary>
    public int RCapitalKey { get; init; }
}
