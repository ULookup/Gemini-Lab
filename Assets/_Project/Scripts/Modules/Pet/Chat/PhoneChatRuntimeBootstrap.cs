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
            var config = Resources.Load<LLMConfigSO>("LLMConfig");
            if (config == null)
            {
                Debug.Log("[PhoneChat] LLMConfigSO not found in Resources, chat will use fallback only");
                config = ScriptableObject.CreateInstance<LLMConfigSO>();
            }

            if (!ServiceLocator.TryResolve<IPetChatService>(out _))
            {
                _ = new PetChatService(config);
            }

            if (!ServiceLocator.TryResolve<IChatPersistenceService>(out _))
            {
                _ = new ChatPersistenceService();
            }
        }
    }
}
