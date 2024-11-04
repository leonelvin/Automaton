using Dalamud.Game.Network.Structures;

namespace Automaton.Utilities;
public class Events
{
    public static event Action<uint, uint, uint>? AchievementProgressUpdate;
    public static void OnAchievementProgressUpdate(uint id, uint current, uint max) => AchievementProgressUpdate?.Invoke(id, current, max);

    public static event Action<nint, nint, nint, byte>? PacketSent;
    public static void OnPacketSent(nint addon, nint opcode, nint data, byte result) => PacketSent?.Invoke(addon, opcode, data, result);

    public static event Action<nint, uint, nint>? PacketReceived;
    public static void OnPacketRecieved(nint addon, uint opcode, nint data) => PacketReceived?.Invoke(addon, opcode, data);

    public static event Action? ListingsStart;
    public static void OnListingsStart() => ListingsStart?.Invoke();

    public static event Action<IReadOnlyList<IMarketBoardItemListing>>? ListingsPage;
    public static void OnListingsPage(IReadOnlyList<IMarketBoardItemListing> itemListings) => ListingsPage?.Invoke(itemListings);

    public static event Action<List<IMarketBoardItemListing>>? ListingsEnd;
    public static void OnListingsEnd(List<IMarketBoardItemListing> listings) => ListingsEnd?.Invoke(listings);
}
