namespace Automaton.Features;

[Tweak(debug: true)]
internal class FlightBypass : Tweak
{
    public override string Name => "Flight Bypass";
    public override string Description => "Bypasses flight restrictions in all zones where it's possible to fly.";

    public override void Enable() => P.Memory.IsFlightProhibitedHook.Enable();
    public override void Disable() => P.Memory.IsFlightProhibitedHook.Disable();
}
