#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    public static class PhoneChatRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Debug.Log("[PhoneChat] Bootstrap started");

            var config = Resources.Load<LLMConfigSO>("LLMConfig");
            if (config == null)
            {
                Debug.Log("[PhoneChat] LLMConfigSO not found in Resources, chat will use fallback only");
                config = ScriptableObject.CreateInstance<LLMConfigSO>();
            }
            else
            {
                Debug.Log($"[PhoneChat] LLMConfig loaded: endpoint={config.Endpoint}, model={config.Model}, configured={config.IsConfigured}");
            }

            if (!ServiceLocator.TryResolve<IPetChatService>(out _))
            {
                var svc = new PetChatService(config);
                Debug.Log($"[PhoneChat] PetChatService registered (configured={config.IsConfigured})");
            }
            else
            {
                Debug.Log("[PhoneChat] IPetChatService already registered");
            }

            if (!ServiceLocator.TryResolve<IChatPersistenceService>(out _))
            {
                _ = new ChatPersistenceService();
                Debug.Log("[PhoneChat] ChatPersistenceService registered");
            }

            Debug.Log("[PhoneChat] Bootstrap complete");
        }
    }
}
