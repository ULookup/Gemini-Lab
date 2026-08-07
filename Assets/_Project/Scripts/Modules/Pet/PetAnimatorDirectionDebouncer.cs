#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 动画朝向去抖器。
    /// 宠物漫游时用 Rigidbody2D 的 velocity 驱动，撞上家具（非 trigger 碰撞体）后会被
    /// 物理引擎沿表面滑动/挤动，逐帧实际位移方向会抖动。若直接用该方向驱动 MoveDir，
    /// 动画会在 Move_Front / Move_Back / Move_Side 之间乱切换。
    /// 该去抖器要求新方向与当前朝向差异足够大（点积低于 <see cref="KeepDotThreshold"/>），
    /// 且连续稳定 <see cref="PersistFrames"/> 帧后才切换朝向，从而过滤碰撞产生的抖动。
    /// </summary>
    public struct PetAnimatorDirectionDebouncer
    {
        /// <summary>
        /// 候选方向与当前朝向点积的最小阈值。低于此值视为“方向差异足够大”，约等于 45° 夹角。
        /// </summary>
        public const float KeepDotThreshold = 0.70710678f;

        /// <summary>新方向需连续保持的帧数，达到后才被采纳。</summary>
        public const int PersistFrames = 6;

        private int _consecutiveChangeFrames;

        /// <summary>当前已连续累计的“方向差异足够大”帧数。</summary>
        public int ConsecutiveChangeFrames => _consecutiveChangeFrames;

        /// <summary>清空累计帧数。宠物停止移动/玩家接管时应调用。</summary>
        public void Reset()
        {
            _consecutiveChangeFrames = 0;
        }

        /// <summary>
        /// 每帧调用一次，返回本帧应使用的稳定朝向。
        /// </summary>
        /// <param name="candidate">本帧的候选朝向（应为归一化向量）。</param>
        /// <param name="currentDirection">当前持有的朝向（应为归一化向量）。</param>
        public Vector2 Step(Vector2 candidate, Vector2 currentDirection)
        {
            if (currentDirection.sqrMagnitude < 0.000001f)
            {
                // 尚无有效朝向时立即采纳首个候选，避免起步阶段朝向错误。
                _consecutiveChangeFrames = 0;
                return candidate;
            }

            if (Vector2.Dot(candidate, currentDirection) >= KeepDotThreshold)
            {
                // 与当前朝向足够接近，视为同一方向，重置累计帧数。
                _consecutiveChangeFrames = 0;
                return currentDirection;
            }

            _consecutiveChangeFrames++;
            if (_consecutiveChangeFrames >= PersistFrames)
            {
                _consecutiveChangeFrames = 0;
                return candidate;
            }

            return currentDirection;
        }
    }
}
