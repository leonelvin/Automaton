using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Excel.GeneratedSheets;
using GC = ECommons.ExcelServices.GrandCompany;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Automaton.Features;
public class CommandsConfiguration
{
    [BoolConfig(Label = "/tpflag")]
    public bool EnableTPFlag = false;

    [BoolConfig(Label = "/tpgc")]
    public bool EnableTPGC = false;

    //[BoolConfig(Label = "/tplast")]
    //public bool EnableTPLast = false;

    //[BoolConfig(Label = "/tpquest")]
    //public bool EnableTPQuest = false;

    [BoolConfig(Label = "/equip")]
    public bool EnableEquip = false;

    [BoolConfig(Label = "/desynth")]
    public bool EnableDesynth = false;

    [BoolConfig(Label = "/lowerquality")]
    public bool EnableLowerQuality = false;

    [BoolConfig(Label = "/item")]
    public bool EnableUseItem = false;

    [BoolConfig(Label = "/directreturn")]
    public bool EnableDirectReturn = false;
}

[Tweak]
public partial class Commands : Tweak<CommandsConfiguration>
{
    public override string Name => "Commands";
    public override string Description => "Miscellanous commands";

    #region Teleport Flag
    [CommandHandler(["/tpf", "/tpflag"], "Teleport to the aetheryte nearest your flag", nameof(Config.EnableTPFlag))]
    internal void OnCommmandTeleportFlag(string command, string arguments)
    {
        Coords.TeleportToAetheryte(Coords.GetNearestAetheryte(Player.MapFlag));
    }
    #endregion

    #region Teleport GC
    [CommandHandler("/tpgc", "Teleport to the aetheryte of your grand company", nameof(Config.EnableTPGC))]
    internal void OnCommmandTeleportGC(string command, string arguments)
    {
        switch (Player.GrandCompany)
        {
            case GC.Maelstrom:
                Svc.Commands.ProcessCommand($"/tp {GetRow<Aetheryte>(8)!.PlaceName.Value!.Name}");
                break;
            case GC.TwinAdder:
                Svc.Commands.ProcessCommand($"/tp {GetRow<Aetheryte>(2)!.PlaceName.Value!.Name}");
                break;
            case GC.ImmortalFlames:
                Svc.Commands.ProcessCommand($"/tp {GetRow<Aetheryte>(9)!.PlaceName.Value!.Name}");
                break;
            default:
                ModuleMessage("No Grand Company");
                break;
        }
    }
    #endregion

    //#region Teleport Quest
    //[CommandHandler(["/tpq", "/tpquest"], "Teleport to the aetheryte nearest your current quest", nameof(Config.EnableTPQuest))]
    //internal void OnCommmandTeleportQuest(string command, string arguments)
    //{
    //    var quest = Player.QuestLocations.FirstOrDefault();
    //    if (Player.QuestLocations.Count != 0)
    //    {
    //        var aetheryte = Coords.GetNearestAetheryte(quest);
    //        Svc.Log.Info($"Teleporting to {GetRow<Aetheryte>(aetheryte).AethernetName.Value.Name.RawString} for quest in {GetRow<TerritoryType>(quest.TerritoryTypeId).PlaceName.Value.Name.RawString}");
    //        Coords.TeleportToAetheryte(aetheryte);
    //    }
    //}
    //#endregion

    #region Equip
    [CommandHandler("/equip", "Equip an item by ID", nameof(Config.EnableEquip))]
    internal unsafe void OnCommmandEquip(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId)) return;
        Player.Equip(itemId);
    }
    #endregion

    #region Desynth
    [CommandHandler("/desynth", "Desynth an item by ID", nameof(Config.EnableDesynth))]
    internal unsafe void OnCommmandDesynth(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId)) return;
        var item_loc = Inventory.GetItemLocationInInventory(itemId, Inventory.Equippable);
        if (item_loc == null)
        {
            DuoLog.Error($"Failed to find item {GetRow<Item>(itemId)?.Name} (ID: {itemId}) in inventory");
            return;
        }

        var item = InventoryManager.Instance()->GetInventoryContainer(item_loc.Value.inv)->GetInventorySlot(item_loc.Value.slot);
        if (GetRow<Item>(item->ItemId)!.Desynth == 0)
        {
            DuoLog.Error($"Item {GetRow<Item>(item->ItemId)?.Name} (ID: {item->ItemId}) is not desynthable");
            return;
        }

        P.Memory.SalvageItem(AgentSalvage.Instance(), item, 0, 0);
        var retval = new AtkValue();
        Span<AtkValue> param = [
            new AtkValue { Type = ValueType.Int, Int = 0 },
            new AtkValue { Type = ValueType.Bool, Byte = 1 }
        ];
        AgentSalvage.Instance()->AgentInterface.ReceiveEvent(&retval, param.GetPointer(0), 2, 1);
    }
    #endregion

    #region Lower Quality
    [CommandHandler("/lowerquality", "Lower the quality of an item by ID, or pass all", nameof(Config.EnableLowerQuality))]
    internal unsafe void OnCommmandLowerQuality(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId) && arguments != "all") return;
        if (arguments == "all")
        {
            foreach (var i in Inventory.GetHQItems(Inventory.PlayerInventory))
            {
                Svc.Log.Info($"Lowering quality on item [{i.Value->ItemId}] {GetRow<Item>(i.Value->ItemId)?.Name} in {i.Value->Container} slot {i.Value->Slot}");
                TaskManager.EnqueueDelay(20);
                TaskManager.Enqueue(() => AgentInventoryContext.Instance()->LowerItemQuality(i.Value, i.Value->Container, i.Value->Slot, 0));
            }
        }
        else
        {
            var item = Inventory.GetItemInInventory(itemId, Inventory.PlayerInventory, true);
            if (item != null)
            {
                Svc.Log.Info($"Lowering quality on item [{item->ItemId}] {GetRow<Item>(item->ItemId)?.Name} in {item->Container} slot {item->Slot}");
                AgentInventoryContext.Instance()->LowerItemQuality(item, item->Container, item->Slot, 0);
            }
        }
    }
    #endregion

    #region Use Item
    [CommandHandler("/item", "Use an item by ID", nameof(Config.EnableUseItem))]
    internal unsafe void OnCommandUseItem(string command, string arguments)
    {
        if (!uint.TryParse(arguments, out var itemId)) return;
        var agent = ActionManager.Instance();
        if (agent == null) return;

        agent->UseAction(itemId >= 2_000_000 ? ActionType.KeyItem : ActionType.Item, itemId, extraParam: 65535);
    }
    #endregion

    #region Direct Return
    [CommandHandler("/directreturn", "Calls the return function directly. Use this over the bypass if the other didn't work for you.", nameof(Config.EnableDirectReturn))]
    internal unsafe void OnCommandDirectReturn(string command, string arguments)
    {
        var agent = ActionManager.Instance();
        if (agent == null) return;
        agent->UseActionLocation(ActionType.GeneralAction, 8);
    }
    #endregion
}
