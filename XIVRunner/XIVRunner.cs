﻿using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;

namespace XIVRunner;

/// <summary>
/// Make your character can be automatically moved for FFXIV in Dalamud.
/// </summary>
public class XIVRunner : IDisposable
{
    private readonly OverrideMovement _movementManager;
    private readonly OverrideAFK _overrideAFK;

    /// <summary>
    /// The Navigate points.
    /// </summary>
    public Queue<Vector3> NaviPts { get; } = new Queue<Vector3>(64);

    /// <summary>
    /// Auto run along the pts.
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// If the users control the movement, what will you do?
    /// You can modify <see cref="NaviPts"/> at this time.
    /// </summary>
    public System.Action? ActionIfUserInput
    {
        get => _movementManager.ActionIfUserInput;
        set => _movementManager.ActionIfUserInput = value;
    }

    /// <summary>
    /// If the player is close enough to the point, It'll remove the pt.
    /// </summary>
    public float Precision
    {
        get => _movementManager.Precision;
        set => _movementManager.Precision = value;
    }

    /// <summary>
    /// During it is auto running, what actions you want to do? Sprint maybe.
    /// This action will be invoked every frame while it is auto running.
    /// </summary>
    public System.Action? RunFastAction { get; set; }

    /// <summary>
    /// Use mount?.
    /// </summary>
    /// 
    public bool UseMount { get; set; }

    /// <summary>
    /// The mount id.
    /// </summary>
    public uint? MountId { get; set; }

    /// <summary>
    /// Is player flying.
    /// </summary>
    public static bool IsFlying => Service.Condition[ConditionFlag.InFlight] || Service.Condition[ConditionFlag.Diving];

    /// <summary>
    /// Is player mounted.
    /// </summary>
    public static bool IsMounted => Service.Condition[ConditionFlag.Mounted];

    /// <summary>
    /// Is this moving is valid
    /// </summary>
    public bool MovingValid { get; private set; } = true;

    /// <summary>
    /// The way to create this.
    /// </summary>
    /// <param name="pluginInterface"></param>
    /// <returns></returns>
    public static XIVRunner Create(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        return new XIVRunner();
    }

    private XIVRunner()
    {
        _movementManager = new OverrideMovement();
        _overrideAFK = new OverrideAFK();
        Service.Framework.Update += Update;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _movementManager.Dispose();
        Service.Framework.Update -= Update;
        GC.SuppressFinalize(this);
    }

    private void Update(IFramework framework)
    {
        if (Service.ClientState.LocalPlayer == null) return;
        if (Service.Condition == null || !Service.Condition.Any()) return;

        UpdateDirection();
        CheckIsRunning();
    }

    private const int POSITION_CAPACITY = 10;
    private readonly Queue<Vector3> _positions = new(POSITION_CAPACITY);
    private DateTime _checkTime = DateTime.MinValue;
    private bool _jumped = false;
    private void CheckIsRunning()
    {
        if (Service.ClientState.LocalPlayer == null) return;
        if (DateTime.Now < _checkTime) return;
        _checkTime = DateTime.Now + TimeSpan.FromSeconds(0.1);

        MovingValid = true;

        if (_movementManager.DesiredPosition == null)
        {
            _positions.Clear();
            return;
        }

        while (_positions.Count >= POSITION_CAPACITY)
        {
            _positions.TryDequeue(out _);
        }

        var playerPos = Service.ClientState.LocalPlayer.Position;
        _positions.Enqueue(playerPos);

        if (_positions.Count != POSITION_CAPACITY) return;

        var firstPt = _positions.Peek();

        if ((playerPos - firstPt).LengthSquared() >= 0.4)
        {
            _jumped = false;
            return;
        }

        if (!_jumped)
        {
            ExecuteJump();
            _jumped = true;
            _checkTime = DateTime.Now + TimeSpan.FromSeconds(1.1);
        }
        else
        {
            NaviPts.Clear();
            Service.Log.Warning("Runner seems didn't run well, please check if the NaviPts isn't correct!");

            MovingValid = false;
        }
    }

    private void UpdateDirection()
    {
        var position = Service.ClientState.LocalPlayer?.Position ?? default;

        if (!Enable)
        {
            _movementManager.DesiredPosition = null;
            return;
        }

    GetPT:
        if (NaviPts.Any())
        {
            var target = NaviPts.Peek();

            var dir = target - position;
            if (IsFlying ? dir.Length() < Precision
                : new Vector2(dir.X, dir.Z).Length() < Precision)
            {
                NaviPts.Dequeue();
                goto GetPT;
            }

            WhenFindTheDesirePosition(target);
        }
        else
        {
            WhenNotFindTheDesirePosition();
        }
    }

    private void WhenFindTheDesirePosition(Vector3 target)
    {
        _overrideAFK.ResetTimers();
        if (_movementManager.DesiredPosition != target)
        {
            _movementManager.DesiredPosition = target;
            TryMount();
        }
        else
        {
            TryFly();
            TryRunFast();
        }
    }

    private void WhenNotFindTheDesirePosition()
    {
        if (_movementManager.DesiredPosition != null)
        {
            _movementManager.DesiredPosition = null;
            if (IsMounted && !IsFlying)
            {
                ExecuteDismount();
            }
        }
    }

    private static readonly Dictionary<ushort, bool> canFly = new();
    private static void TryFly()
    {
        if (Service.Condition[ConditionFlag.Jumping]) return;
        if (IsFlying) return;
        if (!IsMounted) return;

        bool hasFly = canFly.TryGetValue(Service.ClientState.TerritoryType, out var fly);

        //TODO: Whether it is possible to fly from the current territory.
        if (fly || !hasFly)
        {
            ExecuteJump();

            if (!hasFly)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(200);
                    canFly[Service.ClientState.TerritoryType] = IsFlying;
                });
            }
        }
    }

    private void TryRunFast()
    {
        if (IsMounted) return;

        try
        {
            RunFastAction?.Invoke();
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, $"Your action, {nameof(RunFastAction)}, run failed.");
        }
    }

    private void TryMount()
    {
        if (IsMounted || !UseMount) return;

        var territory = Service.Data.GetExcelSheet<TerritoryType>()?.GetRow(Service.ClientState.TerritoryType);
        if (territory?.Mount ?? false)
        {
            ExecuteMount();
        }
    }

    private static unsafe bool ExecuteActionSafe(ActionType type, uint id)
        => ActionManager.Instance()->GetActionStatus(type, id) == 0
        && ActionManager.Instance()->UseAction(type, id);

    private bool ExecuteMount()
    {
        if (MountId.HasValue && ExecuteActionSafe(ActionType.Mount, MountId.Value))
        {
            return true;
        }
        else
        {
            return ExecuteActionSafe(ActionType.GeneralAction, 9);
        }
    }
    private static bool ExecuteDismount() => ExecuteActionSafe(ActionType.GeneralAction, 23);
    private static bool ExecuteJump() => ExecuteActionSafe(ActionType.GeneralAction, 2);
}
