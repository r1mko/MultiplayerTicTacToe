using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell: MonoBehaviour
{
    [SerializeField] private Button cellButton;
    [SerializeField] private GameObject[] fillView;
    [SerializeField] private Color preDestroyColor;
    [SerializeField] private Color defaultColor;

    private Coroutine blinkCoroutine = null;

    private int _indexPlayer;
    private bool _isFillCell;

    public int IndexPlayer => _indexPlayer;
    public bool IsFillCell => _isFillCell;
    
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
    }

    public void Block()
    {
        cellButton.interactable = false;
    }

    public void Unblock()
    {
        cellButton.interactable = true;
    }
    

    internal void PreDestroy()
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
        float duration = 0.5f; // Время одного полного цикла "мигания" (туда-обратно)
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

    public bool IsSameCell(int indexPlayer)
    {
        if (!IsFillCell)
        {
            return false;
        }
        return indexPlayer == IndexPlayer;
    }
    private void HideAll()
    {
        foreach (var item in fillView)
        {
            item.SetActive(false);
        }
    }

    public void Init(int row, int col)
    {
        Clear();
        Unblock();
        cellButton.onClick.AddListener(() => BoardManager.Singltone.OnClickCell(row, col, this));
    }


    private void OnDestroy()
    {
        cellButton.onClick.RemoveAllListeners();
    
}
}
