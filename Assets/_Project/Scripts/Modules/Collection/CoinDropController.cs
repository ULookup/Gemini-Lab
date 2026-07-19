#nullable enable
using System.Collections;
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 挂在 Pet 父节点上，每 10 秒在宠物位置掉落金币。
    /// 玩家点击金币 → 收集；5 秒超时自动销毁。
    /// </summary>
    public sealed class CoinDropController : MonoBehaviour
    {
        [SerializeField] private float _intervalSeconds = 10f;
        [SerializeField] private int _minAmount = 10;
        [SerializeField] private int _maxAmount = 40;
        [SerializeField] private float _lifetimeSeconds = 5f;
        [SerializeField] private Vector2 _randomOffset = new(0.5f, 0.5f);
        [SerializeField] private string _coinSpritePath = "Sprites/Collection/collection_system/apple";
        [SerializeField] private Transform[] _petTargets;
        private ICoinService? _coinService;
        private Coroutine? _dropRoutine;

        private void Start()
        {
            ServiceLocator.TryResolve(out _coinService);
            if (_coinService == null)
            {
                Debug.LogError("[CoinDropController] 找不到 ICoinService，所以脚本被禁用了", this);
                enabled = false;
                return;
            }
            _dropRoutine = StartCoroutine(DropLoop());
        }

        private void OnDestroy()
        {
            if (_dropRoutine != null) StopCoroutine(_dropRoutine);
        }

        private IEnumerator DropLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_intervalSeconds);
                SpawnCoin();
            }
        }

        private void SpawnCoin()
        {
            if (_petTargets == null || _petTargets.Length == 0) return;

            Transform pet = _petTargets[Random.Range(0, _petTargets.Length)];
            int amount = Random.Range(_minAmount, _maxAmount + 1);
            var sprite = Resources.Load<Sprite>(_coinSpritePath);
            if (sprite == null) return;

            Vector3 pos = transform.position;
            pos.x += Random.Range(-_randomOffset.x, _randomOffset.x);
            pos.y += Random.Range(-_randomOffset.y, _randomOffset.y);

            var go = new GameObject($"Coin_{amount}");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var collector = go.AddComponent<CoinCollector>();
            collector.Init(_coinService!, amount, _lifetimeSeconds);
        }
    }

    /// <summary>单个金币的点击收集 + 超时销毁。</summary>
    public sealed class CoinCollector : MonoBehaviour
    {
        private ICoinService _coinService = null!;
        private int _amount;
        private float _elapsed;

        public void Init(ICoinService coinService, int amount, float lifetime)
        {
            _coinService = coinService;
            _amount = amount;
            Destroy(gameObject, lifetime);
        }

        private void OnMouseDown()
        {
            _coinService.Add(_amount);
            ServiceLocator.TryResolve(out Core.Events.EventBus? eb);
            eb?.Publish(new CoinCollectedEvent(_amount));
            Destroy(gameObject);
        }
    }
}
