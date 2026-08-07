#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    /// <summary>
    /// 验证漫游宠物撞上家具后动画方向的去抖行为。
    /// 场景：宠物沿家具表面被物理引擎滑动/挤动时，逐帧实际位移方向会抖动，
    /// 若直接驱动 MoveDir 会让动画在 Move_Front/Back/Side 间乱切换。
    /// </summary>
    public sealed class PetAnimatorDirectionDebouncerTests
    {
        private static readonly Vector2 Right = Vector2.right;
        private static readonly Vector2 Up = Vector2.up;
        private static readonly Vector2 Down = Vector2.down;

        [Test]
        public void OscillatingCandidate_NeverReachesPersistCount_KeepsHeldDirection()
        {
            // 模拟宠物在家具边缘抖动：方向在“朝右”和“朝上”之间来回，
            // 任何一种方向都无法连续稳定 6 帧。
            var debouncer = new PetAnimatorDirectionDebouncer();
            Vector2 held = Right;

            for (int i = 0; i < 30; i++)
            {
                Vector2 candidate = i % 2 == 0 ? Up : Right;
                held = debouncer.Step(candidate, held);
                Assert.AreEqual(Right, held, $"第 {i} 帧方向不应因抖动而改变");
            }
        }

        [Test]
        public void PersistentNewDirection_IsAdoptedAfterPersistFrames()
        {
            var debouncer = new PetAnimatorDirectionDebouncer();
            Vector2 held = Right;

            // 连续 5 帧朝上：仍应保持朝右（未达到连续帧数阈值）。
            for (int i = 0; i < PetAnimatorDirectionDebouncer.PersistFrames - 1; i++)
            {
                held = debouncer.Step(Up, held);
                Assert.AreEqual(Right, held, $"第 {i} 帧尚未达到阈值，不应切换");
            }

            // 第 6 帧：达到阈值，应切换到朝上。
            held = debouncer.Step(Up, held);
            Assert.AreEqual(Up, held, "达到连续帧数阈值后应采纳新方向");
        }

        [Test]
        public void CandidateWithinKeepCone_DoesNotChangeDirection()
        {
            // 与当前朝向夹角约 26°（点积 0.9），属于可接受抖动范围。
            var debouncer = new PetAnimatorDirectionDebouncer();
            Vector2 candidate = new Vector2(0.9f, 0.435f).normalized;

            Vector2 held = debouncer.Step(candidate, Right);

            Assert.AreEqual(Right, held, "夹角在阈值内的候选方向不应触发切换");
        }

        [Test]
        public void SingleBounce_DoesNotAccumulateTowardDirectionFlip()
        {
            var debouncer = new PetAnimatorDirectionDebouncer();
            Vector2 held = Right;

            // 一次“朝上”抖动后立刻回到“朝右”：计数应被重置。
            held = debouncer.Step(Up, held);
            Assert.AreEqual(Right, held);
            held = debouncer.Step(Right, held);
            Assert.AreEqual(Right, held);

            // 再次“朝上”，需要重新累积 6 帧才会切换。
            for (int i = 0; i < PetAnimatorDirectionDebouncer.PersistFrames - 1; i++)
            {
                held = debouncer.Step(Up, held);
                Assert.AreEqual(Right, held, "回弹后应重新累计帧数");
            }

            held = debouncer.Step(Up, held);
            Assert.AreEqual(Up, held);
        }

        [Test]
        public void Reset_ClearsConsecutiveFrames()
        {
            var debouncer = new PetAnimatorDirectionDebouncer();
            Vector2 held = Right;

            for (int i = 0; i < PetAnimatorDirectionDebouncer.PersistFrames - 1; i++)
            {
                held = debouncer.Step(Up, held);
            }

            Assert.Greater(debouncer.ConsecutiveChangeFrames, 0, "重置前应已累计帧数");

            debouncer.Reset();

            Assert.AreEqual(0, debouncer.ConsecutiveChangeFrames, "Reset 后应清空累计帧数");
        }
    }
}
