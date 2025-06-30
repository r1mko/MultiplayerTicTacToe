using UnityEngine;
using UnityEngine.UI;

public class Cell: MonoBehaviour
{
    [SerializeField] private Button cellButton;
    [SerializeField] private GameObject[] fillView;

    private int _indexPlayer;
    private bool _isFillCell;

    public int IndexPlayer => _indexPlayer;
    public bool IsFillCell => _isFillCell;
    
    public void Clear()
    {
        HideAll();
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

    private void OnClickCell(int row, int col)
    {
        if (!GameManager.Singltone.IsOurTurn())
        {
            return;
        }
        NetworkPlayer.Singletone.OnClickRpc(row, col);
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
        cellButton.onClick.AddListener(() => OnClickCell(row, col));
    }


    private void OnDestroy()
    {
        cellButton.onClick.RemoveAllListeners();
    
}
}
