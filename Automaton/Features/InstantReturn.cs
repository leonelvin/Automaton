namespace Automaton.Features;

[Tweak]
public unsafe class InstantReturn : Tweak
{
    public override string Name => "Return Bypass";
    public override string Description => "Bypass return cast time and cool down. If this does not work for you please enable /directreturn in Commands instead.";

    public override void Enable()
    {
        P.Memory.ReturnHook.Enable();
        P.Memory.ExecuteCommandHook.Enable();
    }

    public override void Disable()
    {
        P.Memory.ReturnHook.Disable();
        P.Memory.ExecuteCommandHook.Disable();
    }
}
