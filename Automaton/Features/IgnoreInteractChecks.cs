using ECommons.GameFunctions;
using ImGuiNET;

namespace Automaton.Features;

[Tweak(debug: true)]
internal class IgnoreInteractChecks : Tweak
{
    public override string Name => "Ignore interact checks";
    public override string Description => "";

    public override void Enable()
    {
        P.Memory.CameraObjectBlockedHook.Enable();
        P.Memory.IsObjectInViewRangeHook.Enable();
        P.Memory.InteractCheckHook.Enable();
        P.Memory.IsPlayerJumping0Hook.Enable();
        //P.Memory.IsPlayerJumping1Hook.Enable();
        //P.Memory.IsPlayerJumping2Hook.Enable();
        P.Memory.CheckTargetDistanceHook.Enable();
        P.Memory.EventCancelledHook.Enable();
        P.Memory.CheckTargetPositionHook.Enable();
    }

    public override void Disable()
    {
        P.Memory.CameraObjectBlockedHook.Disable();
        P.Memory.IsObjectInViewRangeHook.Disable();
        P.Memory.InteractCheckHook.Disable();
        P.Memory.IsPlayerJumping0Hook.Disable();
        //P.Memory.IsPlayerJumping1Hook.Disable();
        //P.Memory.IsPlayerJumping2Hook.Disable();
        P.Memory.CheckTargetDistanceHook.Disable();
        P.Memory.EventCancelledHook.Disable();
        P.Memory.CheckTargetPositionHook.Disable();
    }

    private bool objectblocked = false;
    private bool inviewrange = false;
    private bool distance = false;
    private bool interact = false;
    private bool cancelled = false;
    private bool jump = false;
    private bool position = false;
    public override void DrawConfig()
    {
        if (ImGui.Checkbox("CameraObjectBlocked", ref objectblocked))
        {
            if (objectblocked)
                P.Memory.CameraObjectBlockedHook.Enable();
            else
                P.Memory.CameraObjectBlockedHook.Disable();
        }

        if (ImGui.Checkbox("In View Range", ref inviewrange))
        {
            if (inviewrange)
                P.Memory.IsObjectInViewRangeHook.Enable();
            else
                P.Memory.IsObjectInViewRangeHook.Disable();
        }

        if (ImGui.Checkbox("Interact Check", ref interact))
        {
            if (interact)
                P.Memory.InteractCheckHook.Enable();
            else
                P.Memory.InteractCheckHook.Disable();
        }

        if (ImGui.Checkbox("Is Jumping", ref jump))
        {
            if (jump)
            {
                P.Memory.IsPlayerJumping0Hook.Enable();
                //P.Memory.IsPlayerJumping1Hook.Enable();
                //P.Memory.IsPlayerJumping2Hook.Enable();
            }
            else
            {
                P.Memory.IsPlayerJumping0Hook.Disable();
                //P.Memory.IsPlayerJumping1Hook.Disable();
                //P.Memory.IsPlayerJumping2Hook.Disable();
            }
        }

        if (ImGui.Checkbox("Target Distance Check", ref distance))
        {
            if (distance)
                P.Memory.CheckTargetDistanceHook.Enable();
            else
                P.Memory.CheckTargetDistanceHook.Disable();
        }

        if (ImGui.Checkbox("Event Cancelled", ref cancelled))
        {
            if (cancelled)
                P.Memory.EventCancelledHook.Enable();
            else
                P.Memory.EventCancelledHook.Disable();
        }

        if (ImGui.Checkbox("Target Position Check", ref position))
        {
            if (position)
                P.Memory.CheckTargetPositionHook.Enable();
            else
                P.Memory.CheckTargetPositionHook.Disable();
        }
    }
}
