using System;
using UnityEngine;
using UnityEngine.UI;

public class SmileButton : MonoBehaviour
{
    [SerializeField] private int index;
    public Button smileButton;

    private Action<int> OnSendSmile;
    private Action OnDisableAction;
    private void Awake()
    {
        smileButton = GetComponent<Button>();
    }
    public void Init(Action<int> callback, Action onDisableAction)
    {
        Debug.Log("������� Init ������");
        OnSendSmile = callback;
        OnDisableAction = onDisableAction;
        smileButton.onClick.AddListener(SendSmile);
    }

    private void SendSmile()
    {
        Debug.Log("������� ����� SendSmile. ����� OnSendSmile ��������");
        OnSendSmile?.Invoke(index);
        OnDisableAction?.Invoke();
    }


    private void OnDestroy()
    {
        smileButton.onClick.RemoveAllListeners();
    }
}
