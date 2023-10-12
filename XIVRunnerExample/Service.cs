using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace XIVRunner;

internal class Service
{
    [PluginService] public static ISigScanner SigScanner { get; private set; }

    [PluginService] internal static ICondition Condition { get; private set; }

    [PluginService] internal static IFramework Framework { get; private set; }

    [PluginService] internal static IClientState ClientState { get; private set; }

    [PluginService] internal static IPluginLog Log { get; private set; }

    [PluginService] internal static IKeyState KeyState { get; private set; }

    [PluginService] internal static IGameGui GameGui { get; private set; }
}
