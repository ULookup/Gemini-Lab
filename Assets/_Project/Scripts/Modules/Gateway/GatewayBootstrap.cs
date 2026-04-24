#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    public static class GatewayBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterGateway()
        {
            if (ServiceLocator.TryResolve(out IGatewayClient? _))
            {
                return;
            }

            GatewayRuntimeHost? existingHost = UnityEngine.Object.FindFirstObjectByType<GatewayRuntimeHost>();
            if (existingHost is null)
            {
                GameObject host = new(nameof(GatewayRuntimeHost));
                UnityEngine.Object.DontDestroyOnLoad(host);
                existingHost = host.AddComponent<GatewayRuntimeHost>();
            }

            existingHost.Initialize();
            ServiceLocator.Register<IGatewayClient>(existingHost.Client);
            ServiceLocator.Register<IGatewayMessageService>(existingHost.MessageService);
            Debug.Log("[GatewayBootstrap] GatewayClient registered.");
        }
    }

    public sealed class GatewayRuntimeHost : MonoBehaviour
    {
        [SerializeField] private GatewayConfigSO? _config;

        private CancellationTokenSource? _lifetimeCts;
        private Task? _replayLoopTask;
        private GatewayEventRouter? _eventRouter;

        public IGatewayClient Client { get; private set; } = null!;
        public IGatewayMessageService MessageService { get; private set; } = null!;

        public void Initialize()
        {
            if (Client is not null)
            {
                return;
            }

            GatewayConfigSO config = _config ?? LoadConfigFallback();
            Client = new GatewayClient(config, new WebSocketGatewayStream());
            SetupRouterAndServices();
            _lifetimeCts = new CancellationTokenSource();
            _replayLoopTask = ReplayLoopAsync(config, _lifetimeCts.Token);
        }

        private void SetupRouterAndServices()
        {
            if (!ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                eventBus = new EventBus();
                ServiceLocator.Register(eventBus);
            }

            if (!ServiceLocator.TryResolve(out IPetCommandLinkService? commandLinkService))
            {
                commandLinkService = new PetCommandLinkService();
                ServiceLocator.Register(commandLinkService);
            }

            _eventRouter = new GatewayEventRouter(Client, commandLinkService!, eventBus!);
            MessageService = new GatewayMessageService(Client, commandLinkService!, new PromptContextBuilder(), eventBus!);
        }

        private static GatewayConfigSO LoadConfigFallback()
        {
            GatewayConfigSO? resourceConfig = Resources.Load<GatewayConfigSO>("GatewayConfig");
            if (resourceConfig is not null)
            {
                return resourceConfig;
            }

            GatewayConfigSO runtime = ScriptableObject.CreateInstance<GatewayConfigSO>();
            runtime.Environment = GatewayEnvironment.Mock;
            return runtime;
        }

        private async Task ReplayLoopAsync(GatewayConfigSO config, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (Client is not null && config.EnableOfflineQueue)
                    {
                        _ = await Client.ReplayPendingAsync(token).ConfigureAwait(false);
                    }

                    await Task.Delay(config.ReplayIntervalMs, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GatewayRuntimeHost] Replay loop failed: {ex.Message}");
                    await Task.Delay(1000, token).ConfigureAwait(false);
                }
            }
        }

        private void Update()
        {
            _eventRouter?.ProcessPendingEvents();
        }

        private void OnDestroy()
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            if (Client is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _eventRouter?.Dispose();
            _eventRouter = null;
        }
    }
}
