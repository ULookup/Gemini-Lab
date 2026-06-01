using System.Collections;
using UnityEngine;

public class GachaCapsuleWiggle : MonoBehaviour
{
    [Header("移动范围")]
    [SerializeField] private float moveRangeX = 35f;
    [SerializeField] private float moveRangeY = 25f;

    [Header("滚动参数")]
    [SerializeField] private float changeTargetInterval = 0.08f;
    [SerializeField] private float moveSpeed = 18f;
    [SerializeField] private float minRotateSpeed = 360f;
    [SerializeField] private float maxRotateSpeed = 900f;

    private RectTransform rectTransform;
    private Vector2 originPosition;
    private Vector2 targetPosition;

    private float timer;
    private float rotateSpeed;
    private bool isRolling;

    private Coroutine returnCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originPosition = rectTransform.anchoredPosition;
        rotateSpeed = Random.Range(minRotateSpeed, maxRotateSpeed);
    }

    private void Update()
    {
        if (!isRolling) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            PickNewTarget();
        }

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );

        rectTransform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    public void StartRolling()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        isRolling = true;
        rotateSpeed = Random.Range(minRotateSpeed, maxRotateSpeed);

        if (Random.value > 0.5f)
        {
            rotateSpeed *= -1f;
        }

        PickNewTarget();
    }

    public void StopRolling()
    {
        isRolling = false;

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }

        returnCoroutine = StartCoroutine(ReturnToOrigin());
    }

    private void PickNewTarget()
    {
        timer = changeTargetInterval;

        Vector2 offset = new Vector2(
            Random.Range(-moveRangeX, moveRangeX),
            Random.Range(-moveRangeY, moveRangeY)
        );

        targetPosition = originPosition + offset;
    }

    private IEnumerator ReturnToOrigin()
    {
        Vector2 start = rectTransform.anchoredPosition;
        float startRotationZ = rectTransform.localEulerAngles.z;

        float duration = 0.25f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.anchoredPosition = Vector2.Lerp(start, originPosition, smoothT);

            yield return null;
        }

        rectTransform.anchoredPosition = originPosition;
    }
}
