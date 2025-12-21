using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    public int row;
    public int coll;
    public int IndexPlayer => indexPlayer;
    public bool IsFillCell => isFillCell;


    [SerializeField] private Button cellButton;
    [SerializeField] private GameObject[] fillView;
    [SerializeField] private Color preDestroyColor;
    [SerializeField] private Color defaultColor;

    private Coroutine blinkCoroutine = null;
    private bool isFillCell;
    private bool isMarkedForDestruction;
    private int indexPlayer;
    private int cellSetAtTurn;
    private int offset;

    public const int DefaultCellLifeTime = 3;
    private int preDestroyTime => DefaultCellLifeTime - 1;



    public void Init(int row, int coll)
    {
        this.row = row;
        this.coll = coll;
        Clear();
        Unblock();
        cellButton.onClick.AddListener(() => BoardManager.Singltone.OnClickCell(row, coll, this));
    }

    public void Fill(int indexPlayer)
    {
        HideAll();
        Block();

        this.indexPlayer = indexPlayer;
        isFillCell = true;

        for (int i = 0; i < fillView.Length; i++)
        {
            if (indexPlayer == i)
            {
                fillView[i].SetActive(true);
            }
        }
    }

    public void SetCell(int turnIndex)
    {
        cellSetAtTurn = turnIndex;
    }

    public void CheckCellState(int turnIndex)
    {
        isMarkedForDestruction = turnIndex - cellSetAtTurn == preDestroyTime * 2 + GameManager.Singletone.GetOffSet();
        if (isMarkedForDestruction)
        {
            PreDestroy();
        }
        Debug.Log($"Уничтожаем клетку в РЯДУ: {row}, СТОЛБЦЕ: {coll}? {isMarkedForDestruction}");
    }

    //public void MarkForDestruction(bool mark)
    //{
    //    isMarkedForDestruction = mark;
    //}

    public void Clear()
    {
        HideAll();

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        ChangeColorCell(defaultColor);
        cellSetAtTurn = -1;
        isFillCell = false;
        isMarkedForDestruction = false;
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

        //blinkCoroutine = StartCoroutine(BlinkAnimation());
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

        GameObject originalChip = fillView[indexPlayer];
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
