#nullable enable

namespace GeminiLab.Modules.Collection
{
    public readonly struct CoinCollectedEvent
    {
        public int Amount { get; }
        public CoinCollectedEvent(int amount) { Amount = amount; }
    }
}
