#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Tracks interaction frequency to influence target scoring.
    /// </summary>
    public sealed class HabitTagService
    {
        private readonly Dictionary<string, int> _counts = new();

        public void RecordInteraction(string furnitureId)
        {
            _counts.TryGetValue(furnitureId, out int count);
            _counts[furnitureId] = count + 1;
        }

        public float GetPreferenceScore(string furnitureId)
        {
            _counts.TryGetValue(furnitureId, out int count);
            return count * 0.1f;
        }
    }
}
