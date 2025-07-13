using DG.Tweening;
using UnityEngine;

public class SmileView : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;

    private void Start()
    {
        AnimateSpawnAndDestroy();
    }

    private void AnimateSpawnAndDestroy()
    {
        transform.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack))
                .AppendInterval(lifeTime)
                .Append(transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                .OnComplete(() => Destroy(gameObject));
    }
}