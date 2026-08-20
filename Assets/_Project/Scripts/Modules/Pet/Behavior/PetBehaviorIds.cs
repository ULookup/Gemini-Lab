#nullable enable
namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 行为 Id 常量（数值规则文档 §7）。行走不是行为，只是状态机中间状态（§8），不在此列。
    /// </summary>
    public static class PetBehaviorIds
    {
        public const string Idle = "idle";
        public const string Sleep = "sleep";
        public const string WaterFlowers = "water_flowers";
        public const string ReadBooks = "read_books";
        public const string PlayHarp = "play_harp";
        public const string Journaling = "journaling";
        public const string DoorGaze = "door_gaze";
        public const string PlayGames = "play_games";
        public const string Paint = "paint";
    }
}
