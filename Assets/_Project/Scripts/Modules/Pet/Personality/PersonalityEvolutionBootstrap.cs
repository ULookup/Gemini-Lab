#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Personality
{
    /// <summary>
    /// 挂在 Boot.BootstrapRoot。Awake 时读 Inspector 的 PersonalityEvolutionRulesSO
    /// 创建 PersonalityEvolutionService，并注册到 ServiceLocator + Registry。
    /// </summary>
    public sealed class PersonalityEvolutionBootstrap : MonoBehaviour
    {
        [SerializeField] private PersonalityEvolutionRulesSO? _rules;

        private PersonalityEvolutionService? _service;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_rules == null)
            {
                Debug.LogError("[PersonalityEvolutionBootstrap] 未绑定 PersonalityEvolutionRulesSO");
                return;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);
            _service = new PersonalityEvolutionService(_rules, eventBus);
            ServiceLocator.Register<IPersonalityEvolutionService>(_service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(_service);
            }

            Debug.Log("[PersonalityEvolutionBootstrap] PersonalityEvolutionService registered.");
        }

        private void OnDestroy()
        {
            _service?.Dispose();
        }
    }
}
