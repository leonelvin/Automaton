using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Automaton.Features;

[Tweak(disabled: true)]
internal class QuickWorldTravel : Tweak
{
    public override string Name => "Quick World Travel";
    public override string Description => "";

    [CommandHandler("/qtravel", "Quick travel to a given world")]
    private unsafe void OnCommand(string command, string arguments)
    {
        if (uint.TryParse(arguments, out var world))
        {
            var agent = Structs.AgentWorldTravel.Instance();
            agent->WorldToTravel = world;
            P.Memory.WorldTravelSetupInfo((nint)agent, AgentLobby.Instance()->LobbyData.CurrentWorldId, (ushort)world);
            var a2 = 1;
            var a3 = 0;
            var a4 = 1;
            const int a5 = 1;
            P.Memory.WorldTravel(agent, (nint)(&a2), (nint)(&a3), (nint)(&a4), a5);
        }
    }
}
