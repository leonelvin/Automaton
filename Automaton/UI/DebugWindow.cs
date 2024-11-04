using Dalamud.Interface.Windowing;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Automaton.UI;

internal class DebugWindow : Window
{
    public DebugWindow() : base($"{Name} - Debug {P.GetType().Assembly.GetName().Version}###{Name}{nameof(DebugWindow)}")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public static void Dispose() { }

    public override unsafe void Draw()
    {
        for (var i = 0; i < RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Count; i++)
        {
            var atk = RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries[i].Value;
            if (atk == null || (atk->Flags198 & 0b1100_0000) != 0 || atk->HostId != 0) continue;
            ImGui.TextUnformatted($"special addon: {atk->NameString}");
        }
        //for (var i = 0; i < RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count; i++)
        //{
        //    var atk = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[i].Value;
        //    ImGui.TextUnformatted($"{i} {atk == null} {(atk != null ? atk->NameString : "")}");
        //}
        //ImGui.TextUnformatted($"{RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[^1].Value == null}");
        //if (TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var lfg))
        //{
        //    ImGui.TextUnformatted($"{RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[^1].Value == lfg}");
        //}
        //for (var i = 0; i < RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count; i++)
        //{
        //    var atk = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[i].Value;
        //    if (atk == null) continue;
        //    var str = GetAddonStruct(atk);
        //    if (str == null) continue;
        //    ImGuiX.DrawSection($"{atk->NameString} - {str.GetType().Name}");
        //    ImGui.Indent();
        //    foreach (var f in str.GetType().GetFields())
        //    {
        //        var type = f.FieldType;
        //        ImGui.TextUnformatted($"{f.Name}: {f.FieldType.Name} {f.FieldType.IsPointer} {f.GetValue(str)} {f.Attributes}");
        //        if (type.IsPointer)
        //        {
        //            var val = (Pointer)f.GetValue(str);
        //            var unboxed = Pointer.Unbox(val);
        //            try
        //            {
        //                var eType = type.GetElementType();
        //                var ptrObj = SafeMemory.PtrToStructure(new IntPtr(unboxed), eType);
        //            }
        //            catch { }
        //        }
        //    }
        //    ImGui.Unindent();
        //}
    }

    private static readonly Dictionary<string, Type?> AddonMapping = [];
    public static unsafe object? GetAddonStruct(AtkUnitBase* atkUnitBase)
    {
        var addonName = atkUnitBase->NameString;
        object addonObj;
        if (addonName != null && AddonMapping.ContainsKey(addonName))
        {
            if (AddonMapping[addonName] == null)
            {
                addonObj = *atkUnitBase;
            }
            else
            {
                addonObj = Marshal.PtrToStructure(new IntPtr(atkUnitBase), AddonMapping[addonName]);
            }
        }
        else if (addonName != null)
        {
            AddonMapping.Add(addonName, null);

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in a.GetTypes())
                    {
                        if (!t.IsPublic) continue;
                        var xivAddonAttr = (Addon)t.GetCustomAttribute(typeof(Addon), false);
                        if (xivAddonAttr == null) continue;
                        if (!xivAddonAttr.AddonIdentifiers.Contains(addonName)) continue;
                        AddonMapping[addonName] = t;
                        break;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            addonObj = *atkUnitBase;
        }
        else
        {
            addonObj = *atkUnitBase;
        }

        return addonObj;
    }
}
