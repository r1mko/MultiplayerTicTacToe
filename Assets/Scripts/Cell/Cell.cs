using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private Button cellButton;
    [SerializeField] private GameObject[] fillView;
    [SerializeField] private Color preDestroyColor;
    [SerializeField] private Color defaultColor;

    private Coroutine blinkCoroutine = null;

    private int _indexPlayer;
    private bool _isFillCell;

    public int row;
    public int coll;

    public int IndexPlayer => _indexPlayer;
    public bool IsFillCell => _isFillCell;

    private bool _isMarkedForDestruction;

    public void Init(int row, int coll)
    {
        this.row = row;
        this.coll = coll;
        Clear();
        Unblock();
        cellButton.onClick.AddListener(() => BoardManager.Singltone.OnClickCell(row, coll, this));
    }

    public void MarkForDestruction(bool mark)
    {
        _isMarkedForDestruction = mark;
    }

    public void Clear()
    {
        HideAll();

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        ChangeColorCell(defaultColor);

        _isFillCell = false;
        Unblock();
    }

    public void Block()
    {
        cellButton.interactable = false;
    }

    public void Unblock()
    {
        cellButton.interactable = true;
    }


    public void PreDestroy()
    {
        ChangeColorCell(preDestroyColor);

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        blinkCoroutine = StartCoroutine(BlinkAnimation());
    }

    private void ChangeColorCell(Color color)
    {
        foreach (var item in fillView)
        {
            item.GetComponent<TMP_Text>().color = color;
        }
    }

    private IEnumerator BlinkAnimation()
    {

        float duration = 2f; // Время одного полного цикла "мигания" (туда-обратно)
        float halfDuration = duration / 2f;

        while (true)
        {
            // Постепенное исчезновение (прозрачность от 1 до 0)
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
                Color newColor = new Color(fillView[0].GetComponent<TMP_Text>().color.r,
                                           fillView[0].GetComponent<TMP_Text>().color.g,
                                           fillView[0].GetComponent<TMP_Text>().color.b,
                                           alpha);

                foreach (var item in fillView)
                {
                    item.GetComponent<TMP_Text>().color = newColor;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Постепенное появление (прозрачность от 0 до 1)
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float alpha = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                Color newColor = new Color(fillView[0].GetComponent<TMP_Text>().color.r,
                                           fillView[0].GetComponent<TMP_Text>().color.g,
                                           fillView[0].GetComponent<TMP_Text>().color.b,
                                           alpha);

                foreach (var item in fillView)
                {
                    item.GetComponent<TMP_Text>().color = newColor;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    public void Fill(int indexPlayer)
    {
        HideAll();
        Block();

        _indexPlayer = indexPlayer;
        _isFillCell = true;

        for (int i = 0; i < fillView.Length; i++)
        {
            if (indexPlayer == i)
            {
                fillView[i].SetActive(true);
            }
        }
    }

    public void HideAll()
    {
        foreach (var item in fillView)
        {
            item.SetActive(false);
        }
    }

    public GameObject CreateChipVisualCopy()
    {
        if (!IsFillCell) return null;

        GameObject originalChip = fillView[_indexPlayer];
        if (originalChip == null) return null;

        // Создаём дубликат объекта
        GameObject chipCopy = Instantiate(originalChip, originalChip.transform.parent.parent);
        chipCopy.SetActive(true);
        chipCopy.transform.position = originalChip.transform.position;
        chipCopy.transform.rotation = originalChip.transform.rotation;
        chipCopy.transform.localScale = originalChip.transform.localScale;


        var textCopy = chipCopy.GetComponent<TMP_Text>();
        var textOriginal = originalChip.GetComponent<TMP_Text>();
        if (textCopy != null && textOriginal != null)
        {
            textCopy.color = textOriginal.color;
        }

        return chipCopy;
    }

    private void OnDestroy()
    {
        cellButton.onClick.RemoveAllListeners();

    }
}
