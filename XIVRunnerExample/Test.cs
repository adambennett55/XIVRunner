using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using XIVRunner;

namespace XIVRunnerExample;

public class Test : IDalamudPlugin, IDisposable
{
    XIVRunner.XIVRunner _runner;

    public Test(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        _runner = XIVRunner.XIVRunner.Create(pluginInterface);
        Service.Framework.Update += Update;
    }

    public void Dispose()
    {
        _runner?.Dispose();
        Service.Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        if (Service.KeyState[Dalamud.Game.ClientState.Keys.VirtualKey.LCONTROL])
        {
            if(Service.GameGui.ScreenToWorld(ImGui.GetCursorScreenPos(), out var pos))
            {
                _runner.NaviPts.Enqueue(pos);
            }
        }
    }
}
