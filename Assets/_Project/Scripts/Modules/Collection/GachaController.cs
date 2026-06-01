using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.Collection
{
    public class GachaController : MonoBehaviour
    {
        [Header("按钮")]
        [SerializeField] private Button buttonDrawOne;
        [SerializeField] private Button buttonDrawFive;

        [Header("扭蛋球控制")]
        [SerializeField] private GachaCapsuleGroupController capsuleGroup;

        [Header("拉杆动画")]
        [SerializeField] private Animator handleAnimator;

        [Header("扭蛋机动画")]
        [SerializeField] private Animator machineAnimator;

        [Header("奖励弹窗")]
        [SerializeField] private GameObject rewardPopup;

        private bool isDrawing = false;

        private void Start()
        {
            buttonDrawOne.onClick.AddListener(OnClickDrawOne);
            buttonDrawFive.onClick.AddListener(OnClickDrawFive);

            if (rewardPopup != null)
            {
                rewardPopup.SetActive(false);
            }
        }

        private void OnClickDrawOne()
        {
            if (isDrawing) return;

            StartCoroutine(PlayGachaSequence(1));
        }

        private void OnClickDrawFive()
        {
            if (isDrawing) return;

            StartCoroutine(PlayGachaSequence(5));
        }

        private IEnumerator PlayGachaSequence(int drawCount)
        {
            isDrawing = true;

            buttonDrawOne.interactable = false;
            buttonDrawFive.interactable = false;

            // 1. 播放拉杆动画
            if (handleAnimator != null)
            {
                handleAnimator.SetTrigger("Pull");
            }

            yield return new WaitForSeconds(0.65f);

            // 2. 扭蛋机震动动画，可选
            if (machineAnimator != null)
            {
                machineAnimator.SetTrigger("Shake");
            }

            // 3. 扭蛋球开始滚动
            capsuleGroup.StartRolling();

            yield return new WaitForSeconds(1.2f);

            // 4. 扭蛋球停止滚动
            capsuleGroup.StopRolling();

            yield return new WaitForSeconds(0.3f);

            // 5. 这里之后接出货动画
            // PlayDropCapsule();

            yield return new WaitForSeconds(0.5f);

            // 6. 显示奖励弹窗
            ShowRewardPopup(drawCount);

            buttonDrawOne.interactable = true;
            buttonDrawFive.interactable = true;

            isDrawing = false;
        }

        private void ShowRewardPopup(int drawCount)
        {
            if (rewardPopup != null)
            {
                rewardPopup.SetActive(true);
            }

            Debug.Log("显示奖励数量：" + drawCount);
        }
    }
}
