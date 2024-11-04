using Dalamud.Game.Network.Structures;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.Automation;
using ECommons.Singletons;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Automaton.Features;

public class MarketAdjusterConfiguration
{
    [IntConfig(DefaultValue = 1)]
    public int PriceReduction = 1;

    [IntConfig(DefaultValue = 100)]
    public int LowestAcceptablePrice = 100;

    [BoolConfig]
    public bool SeparateNQAndHQ = false;

    [IntConfig(DefaultValue = 0)]
    public int MaxPriceReduction = 0;
}

[Tweak]
public partial class MarketAdjuster : Tweak<MarketAdjusterConfiguration>
{
    public override string Name => "Auto Adjust Retainer Listings";
    public override string Description => "This feature is undergoing functionality and configuration renovations and is subject to heavy changes in the future. Do not speak of this feature.";

    private static Listing? CurrentItem;
    private static Listing? LastItem;
    private static List<Listing>? Listings;
    private static int CurrentItemPrice;
    private static int CurrentMarketLowestPrice;
    private static uint CurrentItemSearchItemID;
    private static bool IsCurrentItemHQ;
    private static unsafe RetainerManager.Retainer* CurrentRetainer;

    public class Listing
    {
        public uint ID;
        public int Price;
        public int MarketLowest;
        public string Retainer = "";
    }

    public override void Enable()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", OnRetainerSellList); // List of items
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", OnRetainerSell);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerSell", OnRetainerSell);
        Events.ListingsStart += OnListingsStart;
        Events.ListingsEnd += OnListingsEnd;
    }

    public override void Disable()
    {
        Svc.AddonLifecycle.UnregisterListener(OnRetainerSellList);
        Svc.AddonLifecycle.UnregisterListener(OnRetainerSell);
        Events.ListingsStart -= OnListingsStart;
        Events.ListingsEnd -= OnListingsEnd;
        TaskManager.Abort();
    }

    public override void DrawConfig()
    {
        base.DrawConfig();
        if (ImGui.Button("cancel"))
            TaskManager.Abort();
    }

    private static bool SearchRunning;
    private void OnListingsStart()
    {
        Svc.Log.Info("search running!");
        SearchRunning = true;
    }

    private void OnListingsEnd(IReadOnlyList<IMarketBoardItemListing> listings)
    {
        Svc.Log.Info("search done!");
        SearchRunning = false;
    }

    private void OnRetainerSell(AddonEvent eventType, AddonArgs addonInfo)
    {
        switch (eventType)
        {
            case AddonEvent.PostSetup:
                if (TaskManager.IsBusy) return;
                TaskManager.Enqueue(ClickComparePrice);
                TaskManager.DefaultConfiguration.AbortOnTimeout = false;
                TaskManager.EnqueueDelay(500);
                TaskManager.Enqueue(GetLowestPrice);
                TaskManager.DefaultConfiguration.AbortOnTimeout = true;
                TaskManager.EnqueueDelay(100);
                TaskManager.Enqueue(FillLowestPrice);
                break;
            case AddonEvent.PreFinalize:
                if (TaskManager.NumQueuedTasks <= 1)
                    TaskManager.Abort();
                break;
        }
    }

    private unsafe void OnRetainerSellList(AddonEvent type, AddonArgs args)
    {
        var activeRetainer = RetainerManager.Instance()->GetActiveRetainer();
        if (CurrentRetainer == null || CurrentRetainer != activeRetainer)
            CurrentRetainer = activeRetainer;
        else
            return;

        for (var i = 0; i < activeRetainer->MarketItemCount; i++)
        {
            EnqueueSingleItem(i);
            CurrentMarketLowestPrice = 0;
        }
    }

    private void EnqueueSingleItem(int index)
    {
        TaskManager.Enqueue(() => !SearchRunning);
        TaskManager.Enqueue(() => ClickSellingItem(index));
        TaskManager.EnqueueDelay(100);
        TaskManager.Enqueue(ClickAdjustPrice);
        TaskManager.EnqueueDelay(100);
        TaskManager.Enqueue(ClickComparePrice);
        TaskManager.EnqueueDelay(500);
        TaskManager.DefaultConfiguration.AbortOnTimeout = false;
        TaskManager.Enqueue(GetLowestPrice);
        TaskManager.DefaultConfiguration.AbortOnTimeout = true;
        TaskManager.EnqueueDelay(100);
        TaskManager.Enqueue(FillLowestPrice);
        TaskManager.EnqueueDelay(800);
    }

    private static unsafe void GetListings()
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerMarket);
        for (var i = 0; i < container->Size; i++)
        {
            var item = container->GetInventorySlot(i);
            if (item == null) continue;
        }
        Enumerable.Range(0, (int)container->Size).Count(i => container->GetInventorySlot(i) != null);
    }

    private static unsafe int GetListingsCount()
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerMarket);
        return container != null ? Enumerable.Range(0, (int)container->Size).Count(i => container->GetInventorySlot(i) != null) : 0;
    }

    private static unsafe bool? ClickSellingItem(int index)
    {
        if (TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && IsAddonReady(addon))
        {
            Callback.Fire(addon, true, 0, index, 1);
            return true;
        }

        return false;
    }

    private static unsafe bool? ClickAdjustPrice()
    {
        if (TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon) && IsAddonReady(addon) && !SearchRunning)
        {
            Callback.Fire(addon, true, 0, 0, 0, 0, 0);
            return true;
        }

        return false;
    }

    private static unsafe bool? ClickComparePrice()
    {
        if (TryGetAddonByName<AtkUnitBase>("RetainerSell", out var addon) && IsAddonReady(addon) && !SearchRunning)
        {
            CurrentItemPrice = addon->AtkValues[5].Int;
            IsCurrentItemHQ = Marshal.PtrToStringUTF8((nint)addon->AtkValues[1].String)!.Contains(''); // hq symbol

            Callback.Fire(addon, true, 4);
            return true;
        }

        return false;
    }

    private unsafe bool? GetLowestPrice()
    {
        if (TryGetAddonByName<AtkUnitBase>("ItemSearchResult", out var addon) && IsAddonReady(addon))
        {
            CurrentItemSearchItemID = AgentItemSearch.Instance()->ResultItemId;
            var searchResult = addon->GetTextNodeById(29)->NodeText.ExtractText();
            if (string.IsNullOrEmpty(searchResult)) return false;

            if (int.Parse(AutoRetainerPriceAdjustRegex().Replace(searchResult, "")) == 0)
            {
                CurrentMarketLowestPrice = 0;
                addon->Close(true);
                return true;
            }

            if (Config.SeparateNQAndHQ && IsCurrentItemHQ)
            {
                var foundHQItem = false;
                for (var i = 1; i <= 12 && !foundHQItem; i++)
                {
                    var listing = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i]->GetAsAtkComponentNode()->Component->UldManager.NodeList;
                    if (listing[13]->GetAsAtkImageNode()->AtkResNode.IsVisible())
                    {
                        var priceText = listing[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                        if (int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""), out CurrentMarketLowestPrice)) foundHQItem = true;
                    }
                }

                if (!foundHQItem)
                {
                    var priceText = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                    if (!int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""), out CurrentMarketLowestPrice)) return false;
                }
            }
            else
            {
                var priceText = addon->UldManager.NodeList[5]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]->GetAsAtkComponentNode()->Component->UldManager.NodeList[10]->GetAsAtkTextNode()->NodeText.ExtractText();
                if (!int.TryParse(AutoRetainerPriceAdjustRegex().Replace(priceText, ""), out CurrentMarketLowestPrice)) return false;
            }

            addon->Close(true);
            return true;
        }

        return false;
    }

    private unsafe bool? FillLowestPrice()
    {
        if (TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var addon) && IsAddonReady(&addon->AtkUnitBase))
        {
            var ui = &addon->AtkUnitBase;
            var priceComponent = addon->AskingPrice;

            if (CurrentMarketLowestPrice - Config.PriceReduction < Config.LowestAcceptablePrice)
            {
                var message = GetSeString("Item is listed lower than minimum price, skipping", SeString.CreateItemLink(CurrentItemSearchItemID, IsCurrentItemHQ ? ItemPayload.ItemKind.Hq : ItemPayload.ItemKind.Normal), CurrentMarketLowestPrice, CurrentItemPrice, Config.LowestAcceptablePrice);
                ModuleMessage(message);

                Callback.Fire((AtkUnitBase*)addon, true, 1);
                ui->Close(true);

                return true;
            }

            if (Config.MaxPriceReduction != 0 && CurrentItemPrice - CurrentMarketLowestPrice > Config.LowestAcceptablePrice)
            {
                var message = GetSeString("Item has exceeded maximum acceptable reduction, skipping", SeString.CreateItemLink(CurrentItemSearchItemID, IsCurrentItemHQ ? ItemPayload.ItemKind.Hq : ItemPayload.ItemKind.Normal), CurrentMarketLowestPrice, CurrentItemPrice, Config.MaxPriceReduction);
                ModuleMessage(message);

                Callback.Fire((AtkUnitBase*)addon, true, 1);
                ui->Close(true);

                return true;
            }

            priceComponent->SetValue(CurrentMarketLowestPrice - Config.PriceReduction);
            Callback.Fire((AtkUnitBase*)addon, true, 0);
            ui->Close(true);

            return true;
        }

        return false;
    }

    private readonly Dictionary<string, string>? resourceData;
    private readonly Dictionary<string, string>? fbResourceData;

    public SeString GetSeString(string key, params object[] args)
    {
        var format = resourceData.TryGetValue(key, out var resValue) ? resValue : fbResourceData.GetValueOrDefault(key);
        var ssb = new SeStringBuilder();
        var lastIndex = 0;

        ssb.AddUiForeground($"[{nameof(Automaton)}]", 34);
        foreach (var match in SeStringRegex().Matches(format).Cast<Match>())
        {
            ssb.AddUiForeground(format[lastIndex..match.Index], 2);
            lastIndex = match.Index + match.Length;

            if (int.TryParse(match.Groups[1].Value, out var argIndex) && argIndex >= 0 && argIndex < args.Length)
            {
                if (args[argIndex] is SeString @seString)
                {
                    ssb.Append(@seString);
                }
                else
                {
                    ssb.AddUiForeground(args[argIndex].ToString(), 2);
                }
            }
        }

        ssb.AddUiForeground(format[lastIndex..], 2);
        return ssb.Build();
    }

    [GeneratedRegex("\\{(\\d+)\\}")]
    private static partial Regex SeStringRegex();

    [GeneratedRegex("[^0-9]")]
    private static partial Regex AutoRetainerPriceAdjustRegex();
}
