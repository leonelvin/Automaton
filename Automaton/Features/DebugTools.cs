using Automaton.IPC;
using Automaton.UI;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.Toast;
using ECommons;
using ECommons.Interop;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;

namespace Automaton.Features;

public class DebugToolsConfiguration
{
    [BoolConfig] public bool AutoVoidIslandRest = false;
    [BoolConfig] public bool EnableTPClick = false;
    [BoolConfig] public bool EnableNoClip = false;

    [FloatConfig(DependsOn = nameof(EnableNoClip), DefaultValue = 0.05f)]
    public float NoClipSpeed = 0.05f;

    [BoolConfig] public bool EnableMoveSpeed = false;
    [BoolConfig] public bool EnableDirectActions = false;
    [BoolConfig] public bool EnableTPMarker = false;
    [BoolConfig] public bool EnableTPOffset = false;
    [BoolConfig] public bool EnableTPAbsolute = false;

    [BoolConfig] public bool DisableKnockback = false;
    [BoolConfig] public bool DisableBewitched = false;
}

[Tweak(true)]
public class DebugTools : Tweak<DebugToolsConfiguration>
{
    public override string Name => "Debug Tools";
    public override string Description => "Tools de Debug";

    public override void Enable()
    {
        Svc.Framework.Update += OnUpdate;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MJICraftSchedule", OnSetup);
    }

    public override void Disable()
    {
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifecycle.UnregisterListener(OnSetup);
    }

    private unsafe void OnSetup(AddonEvent type, AddonArgs args)
    {
        if (!Config.AutoVoidIslandRest) return;
        if (Utils.AgentData->RestCycles.ToHex() != 8321u)
            Utils.SetRestCycles(8321u);
    }

    [CommandHandler("/tpclick", "Teleport to your mouse location on click while CTRL is held.", nameof(Config.EnableTPClick))]
    private void OnTeleportClick(string command, string arguments)
    {
        tpActive ^= true;
        if (tpActive)
            EzConfigGui.WindowSystem.AddWindow(new MousePositionOverlay());
        else
            EzConfigGui.RemoveWindow<MousePositionOverlay>();
        Svc.Toasts.ShowNormal($"TPClick {(tpActive ? "Enabled" : "Disabled")}", new ToastOptions() { Speed = ToastSpeed.Fast });
    }

    //private static readonly string CollisionSig = "E8 ?? ?? ?? ?? 48 8B 47 20 0F 57";
    //private static nint CollisionAddr;
    [CommandHandler("/noclip", "Enable NoClip", nameof(Config.EnableNoClip))]
    private void OnNoClip(string command, string arguments)
    {
        ncActive ^= true;
        Config.NoClipSpeed = float.TryParse(arguments, out var speed) ? speed : Config.NoClipSpeed;
        //try
        //{
        //    if (CollisionAddr != IntPtr.Zero && !Config.EnableNoClip)
        //    {
        //        Config.EnableNoClip = true;
        //        Svc.Log.Debug("Disabling Collision");
        //        SafeMemory.WriteBytes(CollisionAddr, [144, 144, 144, 144, 144]);
        //    }
        //    if (CollisionAddr != IntPtr.Zero && Config.EnableNoClip)
        //    {
        //        Config.EnableNoClip = false;
        //        Svc.Log.Debug("Enabling Collision");
        //        SafeMemory.WriteBytes(CollisionAddr, [232, 114, 4, 0, 0]);
        //    }
        //}
        //catch (Exception e) { e.Log(); }
    }

    [CommandHandler(["/move", "/speed"], "Modify your movement speed", nameof(Config.EnableMoveSpeed))]
    private void OnMoveSpeed(string command, string arguments) => Player.Speed = float.TryParse(arguments, out var speed) ? speed : 1.0f;

    public static bool ShowMouseOverlay;
    private bool IsLButtonPressed;
    private bool tpActive;
    private bool ncActive;
    private unsafe void OnUpdate(IFramework framework)
    {
        if (!Player.Available || Player.Occupied) return;
        ShowMouseOverlay = false;
        if (Config.EnableTPClick && tpActive)
        {
            if (!Framework.Instance()->WindowInactive && IsKeyPressed([LimitedKeys.LeftControlKey, LimitedKeys.RightControlKey]) && Utils.IsClickingInGameWorld())
            {
                ShowMouseOverlay = true;
                var pos = ImGui.GetMousePos();
                if (Svc.GameGui.ScreenToWorld(pos, out var res))
                {
                    if (IsKeyPressed(LimitedKeys.LeftMouseButton))
                    {
                        if (!IsLButtonPressed)
                            Player.Position = res;
                        IsLButtonPressed = true;
                    }
                    else
                        IsLButtonPressed = false;
                }
            }
        }

        if (Config.EnableNoClip && ncActive && !Framework.Instance()->WindowInactive)
        {
            if (Svc.KeyState.GetRawValue(VirtualKey.SPACE) != 0 || IsKeyPressed(LimitedKeys.Space))
            {
                Svc.KeyState.SetRawValue(VirtualKey.SPACE, 0);
                Player.Position = (Player.Object.Position.X, Player.Object.Position.Y + Config.NoClipSpeed, Player.Object.Position.Z).ToVector3();
            }
            if (Svc.KeyState.GetRawValue(VirtualKey.LSHIFT) != 0 || IsKeyPressed(LimitedKeys.LeftShiftKey))
            {
                Svc.KeyState.SetRawValue(VirtualKey.LSHIFT, 0);
                Player.Position = (Player.Object.Position.X, Player.Object.Position.Y - Config.NoClipSpeed, Player.Object.Position.Z).ToVector3();
            }
            if (Svc.KeyState.GetRawValue(VirtualKey.W) != 0 || IsKeyPressed(LimitedKeys.W))
            {
                var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - Player.CameraEx->DirH, Player.Object.Position + new Vector3(0, 0, Config.NoClipSpeed));
                Svc.KeyState.SetRawValue(VirtualKey.W, 0);
                Player.Position = newPoint;
            }
            if (Svc.KeyState.GetRawValue(VirtualKey.S) != 0 || IsKeyPressed(LimitedKeys.S))
            {
                var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - Player.CameraEx->DirH, Player.Object.Position + new Vector3(0, 0, -Config.NoClipSpeed));
                Svc.KeyState.SetRawValue(VirtualKey.S, 0);
                Player.Position = newPoint;
            }
            if (Svc.KeyState.GetRawValue(VirtualKey.A) != 0 || IsKeyPressed(LimitedKeys.A))
            {
                var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - Player.CameraEx->DirH, Player.Object.Position + new Vector3(Config.NoClipSpeed, 0, 0));
                Svc.KeyState.SetRawValue(VirtualKey.A, 0);
                Player.Position = newPoint;
            }
            if (Svc.KeyState.GetRawValue(VirtualKey.D) != 0 || IsKeyPressed(LimitedKeys.D))
            {
                var newPoint = Utils.RotatePoint(Player.Object.Position.X, Player.Object.Position.Z, MathF.PI - Player.CameraEx->DirH, Player.Object.Position + new Vector3(-Config.NoClipSpeed, 0, 0));
                Svc.KeyState.SetRawValue(VirtualKey.D, 0);
                Player.Position = newPoint;
            }
        }
    }

    [CommandHandler("/ada", "Call actions directly.", nameof(Config.EnableDirectActions))]
    private unsafe void OnDirectAction(string command, string arguments)
    {
        try
        {
            var args = arguments.Split(' ');
            var actionType = ParseActionType(args[0]);
            var actionID = uint.Parse(args[1]);
            ActionManager.Instance()->UseActionLocation(actionType, actionID);
        }
        catch (Exception e) { e.Log(); }
    }

    private static ActionType ParseActionType(string input)
    {
        if (Enum.TryParse(input, true, out ActionType result))
            return result;

        if (byte.TryParse(input, out var intValue))
            if (Enum.IsDefined(typeof(ActionType), intValue))
                return (ActionType)intValue;

        throw new ArgumentException("Invalid ActionType", nameof(input));
    }

    [CommandHandler("/tpmarker", "Teleport to a given marker", nameof(Config.EnableTPMarker))]
    private unsafe void OnTeleportMarker(string command, string arguments)
    {
        if (int.TryParse(arguments, out var i))
        {
            var m = MarkingController.Instance()->FieldMarkers[i];
            Vector3? pos = m.Active ? new(m.X / 1000.0f, m.Y / 1000.0f, m.Z / 1000.0f) : null;
            if (pos != null)
                Player.Position = (Vector3)pos;
        }
    }

    [CommandHandler("/tpoff", "Teleport from your current position, offset by arguments", nameof(Config.EnableTPOffset))]
    private unsafe void OnTeleportOffset(string command, string arguments)
    {
        if (arguments.TryParseVector3(out var v))
            Player.Position += v;
    }

    [CommandHandler("/tpabs", "Teleport to a given absolute position", nameof(Config.EnableTPAbsolute))]
    private unsafe void OnTeleportAbsolute(string command, string arguments)
    {
        if (arguments.TryParseVector3(out var v))
            Player.Position = v;
    }

    private bool noKb;
    [CommandHandler("/tkb", "Toggle knockback immunity", nameof(Config.EnableNoClip))]
    private void OnToggleKnockback(string command, string arguments)
    {
        noKb ^= true;
        if (noKb)
            P.Memory.KBProcHook?.Enable();
        else
            P.Memory.KBProcHook?.Disable();
        Svc.Toasts.ShowNormal($"Knockback Immunity {(noKb ? "Enabled" : "Disabled")}");
    }

    private bool noBewitch;
    [CommandHandler("/tbw", "Toggle bewitching immunity", nameof(Config.DisableBewitched))]
    private void OnToggleBewitch(string command, string arguments)
    {
        noBewitch ^= true;
        if (noBewitch)
            P.Memory.BewitchHook?.Enable();
        else
            P.Memory.BewitchHook?.Disable();
        Svc.Toasts.ShowNormal($"Bewitch Immunity {(noBewitch ? "Enabled" : "Disabled")}");
    }
}
