using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Numerics;

namespace XIVRunner;

/// <summary>
/// Make your character can be automatically moved for FFXIV in Dalamud.
/// </summary>
public class XIVRunner : IDisposable
{
    private float? _direction;
    private readonly Queue<Vector3> _naviPts = new Queue<Vector3>(64);
    /// <summary>
    /// The direction that the character should go to.
    /// </summary>
    public float? Direction
    {
        get  => _direction;
        set
        {
            if (value == _direction) return;

            _direction = value.HasValue
                ? Mod(value.Value + MathF.PI, MathF.Tau) - MathF.PI
                : null;

            float Mod(float a, float b) => (a % b + b) % b;
        }
    }

    private bool IsAutoRunning 
    {
        get => InputManager.IsAutoRunning();
        set
        {
            if (IsAutoRunning == value) return;

            Chat.SendMessage("/automove");
        }
    }

    /// <summary>
    /// Auto run along the pts.
    /// </summary>
    public bool RunAlongPts { get; set; }

    /// <summary>
    /// The way to create this.
    /// </summary>
    /// <param name="pluginInterface"></param>
    /// <returns></returns>
    public static XIVRunner Create(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Chat.Init();
        return new XIVRunner();
    }

    private XIVRunner()
    {
        if (Service.Framework == null) return;
        Service.Framework.Update += Update;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Service.Framework == null) return;
        Service.Framework.Update -= Update;
    }

    /// <summary>
    /// Add a point into navigating points.
    /// </summary>
    /// <param name="pt"></param>
    public void AddNaviPt(Vector3 pt)
    {
        _naviPts.Enqueue(pt);
    }

    /// <summary>
    /// Clear all navigate points.
    /// </summary>
    public void ClearNaviPt()
    {
        _naviPts.Clear();
    }

    private  void Update(IFramework framework)
    {
        if (Service.ClientState?.LocalPlayer == null) return;
        if (Service.Condition == null || !Service.Condition.Any()) return;

        UpdateDirection();
        UpdateCameraAndAutoRun();
    }

    static bool _isDirActive = false;
    private void UpdateDirection()
    {
        if (RunAlongPts)
        {
            _isDirActive = true;
            var positon = Service.ClientState?.LocalPlayer?.Position ?? default;

            GetPT:
            if (_naviPts.Any())
            {
                var target = _naviPts.Peek();
                var dir = ToDir(positon, target);

                if (dir.Length() < 0.1f)
                {
                    _naviPts.Dequeue();
                    goto GetPT;
                }

                Direction = VecToRadius(dir);
            }
        }
        else if (_isDirActive)
        {
            _isDirActive = false;
            Direction = null;
        }
    }

    private static Vector2 To2D(in Vector3 pt) => new Vector2(pt.X, pt.Z);
    private static Vector2 ToDir(in Vector3 from, in Vector3 to)
        => To2D(to) - To2D(from);
    private static float VecToRadius(in Vector2 pt)
    {
        var x = pt.X;
        var y = pt.Y;

        if(y == 0)
        {
            return x > 0 ? MathF.PI / 2 : -MathF.PI / 2;
        }

        var alpha = MathF.Atan(x / y);
        if (y > 0) return alpha;
        return alpha + MathF.PI;
    }

    static bool _isCnAActive = false;
    private unsafe void UpdateCameraAndAutoRun()
    {
        if (Direction.HasValue)
        {
            IsAutoRunning = _isCnAActive = true;
            *(float*)((IntPtr)(void*)CameraManager.Instance()->Camera + 0x130) = Direction.Value;
        }
        else if (_isCnAActive)
        {
            IsAutoRunning = _isCnAActive = false;
        }
    }
}
