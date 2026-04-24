#nullable enable

namespace GeminiLab.Modules.DesktopOverlay
{
    public interface IWindowModeAdapter
    {
        DesktopMode CurrentMode { get; }
        bool IsClickThrough { get; }
        void SetMode(DesktopMode mode);
        void SetClickThrough(bool enabled);
    }

    /// <summary>
    /// Platform-safe adapter; advanced Win32 behavior can be added under this abstraction.
    /// </summary>
    public sealed class WindowModeAdapter : IWindowModeAdapter
    {
        public DesktopMode CurrentMode { get; private set; } = DesktopMode.Apartment;

        public bool IsClickThrough { get; private set; }

        public void SetMode(DesktopMode mode)
        {
            CurrentMode = mode;
        }

        public void SetClickThrough(bool enabled)
        {
            IsClickThrough = enabled;
        }
    }
}
