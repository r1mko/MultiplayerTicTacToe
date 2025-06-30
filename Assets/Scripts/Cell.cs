using UnityEngine;
using UnityEngine.UI;

public class Cell: MonoBehaviour
{
    [SerializeField] private Button cellButton;
    [SerializeField] private GameObject fillView;
    public void Clear()
    {
        fillView.SetActive(false);
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

    public void Fill()
    {
        fillView.SetActive(true);
        Block();
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
