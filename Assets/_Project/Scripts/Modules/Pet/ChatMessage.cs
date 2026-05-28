#nullable enable
using System;

namespace GeminiLab.Modules.Pet
{
    public enum ChatRole
    {
        User = 0,
        Angel = 1,
        Devil = 2
    }

    [Serializable]
    public sealed class ChatMessage
    {
        public ChatRole Role;
        public string Text = string.Empty;
        public long Timestamp;

        public ChatMessage() { }

        public ChatMessage(ChatRole role, string text)
        {
            Role = role;
            Text = text;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [Serializable]
    internal sealed class ChatHistoryWrapper
    {
        public ChatMessage[] messages = Array.Empty<ChatMessage>();
    }
}
