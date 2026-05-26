#nullable enable
using System;

namespace GeminiLab.Modules.Tarot
{
    [Serializable]
    public sealed class TarotSessionRecord
    {
        public string SessionId = string.Empty;
        public string Question = string.Empty;
        public string SessionDateIso = string.Empty;

        public string PastCardId = string.Empty;
        public string PastOrientation = string.Empty;
        public string PastAngelReading = string.Empty;
        public string PastDevilReading = string.Empty;

        public string PresentCardId = string.Empty;
        public string PresentOrientation = string.Empty;
        public string PresentAngelReading = string.Empty;
        public string PresentDevilReading = string.Empty;

        public string FutureCardId = string.Empty;
        public string FutureOrientation = string.Empty;
        public string FutureAngelReading = string.Empty;
        public string FutureDevilReading = string.Empty;

        public int FortuneLevel;
        public string LuckyColor = string.Empty;
        public string LuckyNumber = string.Empty;
        public string LuckyTime = string.Empty;
        public string LuckyAction = string.Empty;
        public string Advice = string.Empty;
    }
}
