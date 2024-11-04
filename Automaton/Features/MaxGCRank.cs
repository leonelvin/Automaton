namespace Automaton.Features;

[Tweak]
internal class MaxGCRank : Tweak
{
    public override string Name => "Expert Bypass";
    public override string Description => "Automatically maxes your GC rank to bypass the expert delivery requirements. Does not bypass rank-restricted item purchases.";

    public override void Enable() => P.Memory.GCRankHook.Enable();
    public override void Disable() => P.Memory.GCRankHook.Disable();
}
