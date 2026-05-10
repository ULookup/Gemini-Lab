#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 默认 Roster 实现；内存字典维护 PetId → PetRuntimeData 映射。
    /// 生命周期：由 <see cref="GameBootstrap"/> 在 Boot 阶段注册到 <see cref="GeminiLab.Core.ServiceLocator"/>，
    /// 跨场景存活；场景内的 <see cref="PetController"/> 在 Awake 时 Register、OnDestroy 时 Unregister。
    /// </summary>
    public sealed class PetRoster : IPetRoster
    {
        private readonly Dictionary<PetId, PetRuntimeData> _pets = new();
        private readonly List<PetId> _order = new();

        public PetRuntimeData? TryGet(PetId id)
        {
            return _pets.TryGetValue(id, out PetRuntimeData? data) ? data : null;
        }

        public IReadOnlyList<PetId> RegisteredPets => _order;

        public void Register(PetId id, PetRuntimeData runtime)
        {
            if (!_pets.ContainsKey(id))
            {
                _order.Add(id);
            }

            _pets[id] = runtime;
        }

        public void Unregister(PetId id)
        {
            if (_pets.Remove(id))
            {
                _order.Remove(id);
            }
        }
    }
}
