#nullable enable

namespace GeminiLab.Modules.Collection
{
    public interface ICoinService
    {
        int Balance { get; }
        void Add(int amount);
        bool TrySpend(int amount);
    }
}
