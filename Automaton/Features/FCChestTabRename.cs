using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Automaton.Features;

public class FcChestTabRenameConfiguration
{
    [StringConfig] public string TabOne = string.Empty;
    [StringConfig] public string TabTwo = string.Empty;
    [StringConfig] public string TabThree = string.Empty;
    [StringConfig] public string TabFour = string.Empty;
    [StringConfig] public string TabFive = string.Empty;
}

[Tweak(outdated: true)]
internal class FCChestTabRename : Tweak
{
    public override string Name => "Custom FC Chest Tab Names";
    public override string Description => "This has been moved to SimpleTweaks.";

    //public override void Enable()
    //{
    //    Svc.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "", PreDraw);
    //}

    //public override void Disable()
    //{
    //    Svc.AddonLifecycle.UnregisterListener(PreDraw);
    //}

    //private unsafe void PreDraw(AddonEvent type, AddonArgs args)
    //{
    //    var addon = (AtkUnitBase*)args.Addon;
    //    if (Config.TabOne != string.Empty)
    //    {
    //        var node = Utils.GetNodeByIDChain(addon->GetRootNode(), 1, 9, 10, 9);
    //        if (node is not null)
    //            node->GetAsAtkTextNode()->NodeText.SetString(Config.TabOne);
    //    }
    //    if (Config.TabTwo != string.Empty)
    //    {
    //        var node = Utils.GetNodeByIDChain(addon->GetRootNode(), 1, 9, 11, 9);
    //        if (node is not null)
    //            node->GetAsAtkTextNode()->NodeText.SetString(Config.TabTwo);
    //    }
    //    if (Config.TabThree != string.Empty)
    //    {
    //        var node = Utils.GetNodeByIDChain(addon->GetRootNode(), 1, 9, 12, 9);
    //        if (node is not null)
    //            node->GetAsAtkTextNode()->NodeText.SetString(Config.TabThree);
    //    }
    //    if (Config.TabFour != string.Empty)
    //    {
    //        var node = Utils.GetNodeByIDChain(addon->GetRootNode(), 1, 9, 13, 9);
    //        if (node is not null)
    //            node->GetAsAtkTextNode()->NodeText.SetString(Config.TabFour);
    //    }
    //    if (Config.TabFive != string.Empty)
    //    {
    //        var node = Utils.GetNodeByIDChain(addon->GetRootNode(), 1, 9, 14, 9);
    //        if (node is not null)
    //            node->GetAsAtkTextNode()->NodeText.SetString(Config.TabFive);
    //    }
    //}
}
