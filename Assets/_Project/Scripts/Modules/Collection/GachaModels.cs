#nullable enable

namespace GeminiLab.Modules.Collection
{
    public readonly struct GachaItem
    {
        public string Id { get; }
        public bool IsNew { get; }
        public GachaItem(string id, bool isNew) { Id = id; IsNew = isNew; }
    }

    public readonly struct GachaResult
    {
        public GachaItem[] Items { get; }
        public int CoinRefund { get; }
        public GachaResult(GachaItem[] items, int coinRefund) { Items = items; CoinRefund = coinRefund; }
    }
}
