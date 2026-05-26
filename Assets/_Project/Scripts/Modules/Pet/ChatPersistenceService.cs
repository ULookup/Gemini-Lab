#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    public interface IChatPersistenceService
    {
        IReadOnlyList<ChatMessage> History { get; }
        void AddMessage(ChatMessage message);
        Task SaveAsync();
        Task LoadAsync();
        void Clear();
    }

    public sealed class ChatPersistenceService : IChatPersistenceService
    {
        private const int MaxMessages = 200;
        private const string FileName = "chat_history.json";

        private readonly List<ChatMessage> _messages = new();
        public IReadOnlyList<ChatMessage> History => _messages;

        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public ChatPersistenceService()
        {
            ServiceLocator.Register<IChatPersistenceService>(this);
        }

        public void AddMessage(ChatMessage message)
        {
            _messages.Add(message);
            while (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var wrapper = new ChatHistoryWrapper { messages = _messages.ToArray() };
                string json = JsonUtility.ToJson(wrapper, prettyPrint: false);
                await File.WriteAllTextAsync(FilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ChatPersistence] Save failed: {ex.Message}");
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                string json = await File.ReadAllTextAsync(FilePath);
                var wrapper = JsonUtility.FromJson<ChatHistoryWrapper>(json);
                if (wrapper?.messages != null)
                {
                    _messages.Clear();
                    _messages.AddRange(wrapper.messages);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ChatPersistence] Load failed: {ex.Message}");
            }
        }

        public void Clear()
        {
            _messages.Clear();
        }
    }
}
