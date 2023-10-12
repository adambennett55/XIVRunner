using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVRunner;

namespace XIVRunnerExample;

public class Test : IDalamudPlugin, IDisposable
{
    XIVRunner.XIVRunner _runner;

    public Test(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        _runner = XIVRunner.XIVRunner.Create(pluginInterface);
        _runner.RunAlongPts = true;
        Service.Framework.Update += Update;
    }

    public void Dispose()
    {
        _runner?.Dispose();
        Service.Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        if (Service.KeyState[Dalamud.Game.ClientState.Keys.VirtualKey.X])
        {
            //_runner.NaviPts.Enqueue(default);
            _runner.NaviPts.Enqueue(Service.ClientState.LocalPlayer.Position
                + new System.Numerics.Vector3((float)(new Random().NextDouble() * 10), 0,
                (float)(new Random().NextDouble() * 10)));
        }
    }
}
