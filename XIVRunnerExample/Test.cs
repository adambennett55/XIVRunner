using Dalamud.Plugin;
using XIVRunner;

namespace XIVRunnerExample;

public class Test : IDalamudPlugin, IDisposable
{
    XIVRunner.XIVRunner _runner;

    public Test(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        _runner = XIVRunner.XIVRunner.Create(pluginInterface);
        _runner.Enable = true;

        //_runner.NaviPts.Enqueue(default);
        _runner.NaviPts.Enqueue(Service.ClientState.LocalPlayer.Position
            + new System.Numerics.Vector3(10, 0,
            0));

        _runner.NaviPts.Enqueue(Service.ClientState.LocalPlayer.Position
               + new System.Numerics.Vector3((float)(new Random().NextDouble() * 10), 0,
               (float)(new Random().NextDouble() * 10)));
    }

    public void Dispose()
    {
        _runner?.Dispose();
    }
}
