#nullable enable
using System.Collections;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.SceneFlow;
using GeminiLab.Core.Time;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Core
{
    /// <summary>
    /// Startup entry responsible for registering core runtime services.
    /// 挂在 Boot.unity 的 BootstrapRoot 上，<see cref="DontDestroyOnLoad"/> 跨场景存活。
    /// 启动完成后会自动发起 Boot → MainMenu 的场景切换。
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static bool _initialized;

        [Tooltip("Boot 完成后自动跳转的目标场景；默认 MainMenu。")]
        [SerializeField] private SceneId _nextScene = SceneId.MainMenu;

        [Tooltip("若在非 Boot 场景（例如直接从 Editor 打开 Apartment 调试）启动，则不进行自动跳转。")]
        [SerializeField] private bool _skipAutoFlowWhenNotInBoot = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() is not null)
            {
                return;
            }

            GameObject go = new(nameof(GameBootstrap));
            DontDestroyOnLoad(go);
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            RegisterCoreServices();
        }

        private IEnumerator Start()
        {
            // 每次启动后先探测一次跨天；后续场景完成时再探一次（延长 session 时也会触发）
            if (ServiceLocator.TryResolve(out IDailyResetService? dailyReset) && dailyReset is not null)
            {
                dailyReset.CheckAndReset();
            }

            if (!ServiceLocator.TryResolve(out ISceneFlowService? sceneFlow))
            {
                yield break;
            }

            string activeName = SceneManager.GetActiveScene().name;
            if (!string.Equals(activeName, "Boot", System.StringComparison.Ordinal))
            {
                SyncCurrentScene(sceneFlow!, activeName);
                if (_skipAutoFlowWhenNotInBoot)
                {
                    yield break;
                }
            }
            else
            {
                if (sceneFlow is SceneFlowService concrete)
                {
                    concrete.SetCurrentScene(SceneId.Boot);
                }
            }

            yield return null;
            sceneFlow!.LoadAsync(_nextScene);
        }

        private static void RegisterCoreServices()
        {
            ServiceLocator.Reset();

            EventBus eventBus = new();
            ServiceLocator.Register(eventBus);
            ServiceLocator.Register(new CommandDispatcher());

            IGameClock clock = new SystemGameClock();
            ServiceLocator.Register(clock);

            ISceneCatalog catalog = new DefaultSceneCatalog();
            SceneFlowService sceneFlow = new(catalog, eventBus);
            ServiceLocator.Register<ISceneCatalog>(catalog);
            ServiceLocator.Register<ISceneFlowService>(sceneFlow);

            ServiceLocator.Register<IUIRouter>(new UIRouter(eventBus));

            var registry = new PersistentServiceRegistry();
            ServiceLocator.Register<IPersistentServiceRegistry>(registry);

            var dailyReset = new DailyResetService(clock, eventBus);
            ServiceLocator.Register<IDailyResetService>(dailyReset);
            registry.Register(dailyReset);
        }

        private static void SyncCurrentScene(ISceneFlowService sceneFlow, string activeName)
        {
            if (sceneFlow is not SceneFlowService concrete)
            {
                return;
            }

            switch (activeName)
            {
                case "MainMenu": concrete.SetCurrentScene(SceneId.MainMenu); break;
                case "Apartment_Main": concrete.SetCurrentScene(SceneId.Apartment); break;
                case "WorldMap_Main": concrete.SetCurrentScene(SceneId.WorldMap); break;
                case "Desktop_Overlay": concrete.SetCurrentScene(SceneId.DesktopOverlay); break;
                default: concrete.SetCurrentScene(SceneId.Boot); break;
            }
        }
    }
}
