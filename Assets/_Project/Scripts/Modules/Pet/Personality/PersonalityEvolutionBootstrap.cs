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
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

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
            SeedInitialMatricesFromActivePets();
        }

        /// <summary>
        /// 启动时补种初始矩阵。编辑器 Play 流程里 Boot 场景可能晚于宠物所在场景加载
        /// （EditorBootSceneLoader 用 delayCall 附加加载 Boot），宠物 Awake 已发布的
        /// <see cref="PetControllerInitializedEvent"/> 会在服务订阅之前被错过，导致
        /// GetMatrix 一直返回全 0。此处扫描当前已激活的宠物补种；已通过正常事件路径
        /// 或存档恢复写入的值会被 ContainsKey 守卫跳过，不会覆盖。
        /// </summary>
        private void SeedInitialMatricesFromActivePets()
        {
            if (_service == null) return;

            var controllers = FindObjectsOfType<PetController>();
            int seeded = 0;
            foreach (var controller in controllers)
            {
                if (controller.InitialPersonalityMatrix is not { } matrix) continue;
                if (_service.SeedInitialMatrixIfAbsent(controller.PetId, matrix)) seeded++;
            }

            if (seeded > 0)
            {
                Debug.Log($"[PersonalityEvolutionBootstrap] 启动补种初始性格: {seeded} 只宠物 (Boot 晚于宠物场景加载，事件已被错过)");
            }
        }

        private void OnDestroy()
        {
            _service?.Dispose();
        }
    }
}
