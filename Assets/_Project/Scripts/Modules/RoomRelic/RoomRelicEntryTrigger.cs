#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 挂在现有 PetMovementBounds / PetMovementBounds_Devil 上的逻辑触发器。
    /// 只负责检测对应宠物进入房间并调用服务，不创建视觉节点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomRelicEntryTrigger : MonoBehaviour
    {
        [SerializeField] private RoomId _roomId = RoomId.AngelRoom;
        [SerializeField] private PetId _expectedPetId = PetId.Angel;

        private void OnTriggerEnter2D(Collider2D other)
        {
            PetController? pet = other.GetComponentInParent<PetController>();
            if (pet == null || pet.PetId != _expectedPetId)
            {
                return;
            }

            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null)
            {
                service.ProcessRoomEntry(_roomId);
                service.SetCurrentRoom(_roomId);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PetController? pet = other.GetComponentInParent<PetController>();
            if (pet == null || pet.PetId != _expectedPetId)
            {
                return;
            }

            if (ServiceLocator.TryResolve(out IRoomRelicService? service) && service is not null)
            {
                service.ClearCurrentRoom(_roomId);
            }
        }
    }
}
