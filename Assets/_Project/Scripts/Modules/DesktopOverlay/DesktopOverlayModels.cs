#nullable enable

namespace GeminiLab.Modules.DesktopOverlay
{
    public enum DesktopMode
    {
        Apartment = 0,
        Overlay = 1
    }

    public readonly struct ForegroundApplicationChangedEvent
    {
        public ForegroundApplicationChangedEvent(string processName)
        {
            ProcessName = processName;
        }

        public string ProcessName { get; }
    }

    public readonly struct OverlayModeChangedEvent
    {
        public OverlayModeChangedEvent(DesktopMode mode)
        {
            Mode = mode;
        }

        public DesktopMode Mode { get; }
    }
}
