#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GeminiLab.Core.Events;
using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    /// <summary>
    /// Polls foreground app context (process-level fallback in editor/runtime).
    /// </summary>
    public sealed class ForegroundWindowProbe : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float _pollIntervalSeconds = 1f;
        private float _nextPollTime;
        private string _lastProcess = string.Empty;
        private EventBus? _eventBus;

        public string CurrentForegroundProcess => _lastProcess;

        private void Awake()
        {
            _ = GeminiLab.Core.ServiceLocator.TryResolve(out _eventBus);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPollTime)
            {
                return;
            }

            if (_eventBus is null)
            {
                _ = GeminiLab.Core.ServiceLocator.TryResolve(out _eventBus);
            }

            _nextPollTime = Time.unscaledTime + _pollIntervalSeconds;
            string processName = GetForegroundProcessName();
            if (string.Equals(processName, _lastProcess, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastProcess = processName;
            _eventBus?.Publish(new ForegroundApplicationChangedEvent(processName));
        }

        private static string GetForegroundProcessName()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                _ = GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId > 0)
                {
                    try
                    {
                        Process process = Process.GetProcessById((int)processId);
                        return process.ProcessName;
                    }
                    catch
                    {
                    }
                }
            }
#endif
            return Process.GetCurrentProcess().ProcessName;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
#endif
    }
}
