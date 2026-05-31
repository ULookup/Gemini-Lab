#nullable enable
using System;

namespace GeminiLab.Core
{
    public sealed class ActionDisposable : IDisposable
    {
        private Action? _dispose;
        public ActionDisposable(Action dispose) => _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}
