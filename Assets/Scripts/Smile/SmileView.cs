using DG.Tweening;
using UnityEngine;

public class SmileView : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;

    private void Start()
    {
        AnimateSarcasticStamp();
    }

    private void AnimateSarcasticStamp()
    {
        transform.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence();

        // 1. Появление: "хлопок" печати — резкий старт с деформацией
        sequence.Append(transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f).SetEase(Ease.OutFlash)) // Хлопок сверху
                 .Append(transform.DOScale(new Vector3(0.9f, 1.1f, 1f), 0.1f).SetEase(Ease.InOutElastic)) // Расплывание
                 .Append(transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack)) // Возвращается к норме

                 // 2. Пауза с эффектом "пуф" — будто осела
                 .AppendInterval(0.05f)

                 // 3. Лёгкое покачивание головы
                 .Append(transform.DORotate(new Vector3(0, 0, -5f), 0.1f).SetEase(Ease.OutSine))
                 .Append(transform.DORotate(new Vector3(0, 0, 3f), 0.1f).SetEase(Ease.InSine))
                 .Append(transform.DORotate(Vector3.zero, 0.05f).SetEase(Ease.OutSine))

                 // 4. Ждём N секунд
                 .AppendInterval(lifeTime)

                 // 5. Исчезновение: "пуф" — как будто смайл сдувается
                 .Append(transform.DOPunchScale(Vector3.one * -0.2f, 0.15f, 1, 1f))
                 .Append(transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InSine))
                 .OnComplete(() => Destroy(gameObject));
    }
}