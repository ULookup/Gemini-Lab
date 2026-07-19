#nullable enable

namespace GeminiLab.Modules.Collection
{
    public readonly struct GachaPullEvent
    {
        public GachaResult Result { get; }
        public GachaPullEvent(GachaResult result) { Result = result; }
    }
}
