#nullable enable

namespace GeminiLab.Modules.Collection
{
    public readonly struct CoinChangedEvent
    {
        public int Balance { get; }
        public CoinChangedEvent(int balance) { Balance = balance; }
    }
}
