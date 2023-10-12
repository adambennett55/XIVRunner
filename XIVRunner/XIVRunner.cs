using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;

namespace XIVRunner;

/// <summary>
/// Make your character can be automatically moved for FFXIV in Dalamud.
/// </summary>
public class XIVRunner : IDisposable
{
    private readonly MovementManager _movementManager;

    /// <summary>
    /// The Navigate points.
    /// </summary>
    public Queue<Vector3> NaviPts { get; } = new Queue<Vector3>(64);

    /// <summary>
    /// Auto run along the pts.
    /// </summary>
    public bool RunAlongPts { get; set; }

    /// <summary>
    /// If the player is close enough to the point, It'll remove the pt.
    /// </summary>
    public float Precision 
    {
        get => _movementManager.Precision;
        set => _movementManager.Precision = value;
    }

    internal bool IsFlying => Service.Condition[ConditionFlag.InFlight] || Service.Condition[ConditionFlag.Diving];
    internal bool IsMounted => Service.Condition[ConditionFlag.Mounted];

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
        _movementManager = new MovementManager();
        Service.Framework.Update += Update;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _movementManager.Dispose();
        Service.Framework.Update -= Update;
    }

    private  void Update(IFramework framework)
    {
        if (Service.ClientState.LocalPlayer == null) return;
        if (Service.Condition == null || !Service.Condition.Any()) return;

        UpdateDirection();
    }

    private void UpdateDirection()
    {
        var positon = Service.ClientState.LocalPlayer?.Position ?? default;

        if (!RunAlongPts || !CanMove())
        {
            _movementManager.DesiredPosition = null;
            return;
        }

    GetPT:
        if (NaviPts.Any())
        {
            var target = NaviPts.Peek();

            var dir = target - positon;
            if (dir.Length() < Precision)
            {
                NaviPts.Dequeue();
                goto GetPT;
            }

            if(_movementManager.DesiredPosition != target)
            {
                _movementManager.DesiredPosition = target;
                TryMount();
            }
            else
            {
                TryFly();
            }
        }
        else if(_movementManager.DesiredPosition != null)
        {
            _movementManager.DesiredPosition = null;
            if (IsMounted) ExecuteMount(); //Try dismount.
        }
    }

    private static readonly Dictionary<ushort, bool> canFly = new Dictionary<ushort, bool>();
    private void TryFly()
    {
        if (Service.Condition[ConditionFlag.Jumping]) return;

        bool hasFly = canFly.TryGetValue(Service.ClientState.TerritoryType, out var fly);

        //TODO: Whether it is possible to fly from the current territory.
        if ((fly || !hasFly) && IsMounted && !IsFlying)
        {
            ExecuteJump();
        }

        if (!hasFly)
        {
            Task.Run(async () =>
            {
                await Task.Delay(200);
                canFly[Service.ClientState.TerritoryType] = IsFlying;
            });
        }
    }

    private void TryMount()
    {
        if (IsMounted) return;

        var territory = Service.Data.GetExcelSheet<TerritoryType>()?.GetRow(Service.ClientState.TerritoryType);
        if (territory?.Mount ?? false)
        {
            ExecuteMount();
        }
    }

    private bool CanMove()
    {
        if(Service.ClientState.LocalPlayer?.IsCasting ?? true) return false;

        if (Service.Condition[ConditionFlag.BetweenAreas] 
            || Service.Condition[ConditionFlag.BetweenAreas51]) return false;

        if (Service.Condition[ConditionFlag.WatchingCutscene]
            || Service.Condition[ConditionFlag.WatchingCutscene78]
            || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent]) return false;

        if (Service.Condition[ConditionFlag.OccupiedInQuestEvent] 
            || Service.Condition[ConditionFlag.OccupiedInEvent] 
            || Service.Condition[ConditionFlag.OccupiedSummoningBell]) return false;

        if (Service.Condition[ConditionFlag.Unknown57]) return false;

        return true;
    }

    private static unsafe void ExecuteActionSafe(ActionType type, uint id)
        => ActionManager.Instance()->UseAction(type, id);
    private void ExecuteMount() => ExecuteActionSafe(ActionType.GeneralAction, 9);
    private void ExecuteJump() => ExecuteActionSafe(ActionType.GeneralAction, 2);
}
