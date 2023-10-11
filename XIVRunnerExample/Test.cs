using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVRunner;

namespace XIVRunnerExample;

public class Test : IDalamudPlugin, IDisposable
{
    XIVRunner.XIVRunner runner;

    public Test(DalamudPluginInterface pluginInterface)
    {
        runner = XIVRunner.XIVRunner.Create(pluginInterface);
        pluginInterface.Create<Service>();
        Service.Framework.Update += Update;

        //Example for moving to the center.
        //runner.AddNaviPt(default);
    }

    public void Dispose()
    {
        runner?.Dispose();
        Service.Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        //Example for moving like a circle.
        //UpdateDirection();
    }

    private void UpdateDirection()
    {
        var second = DateTime.Now.Ticks / 1000f;
        runner.Direction = (second / 5) % 1 * MathF.Tau;
    }
}
