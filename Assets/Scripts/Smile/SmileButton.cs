using System;
using UnityEngine;
using UnityEngine.UI;

public class SmileButton : MonoBehaviour
{
    [SerializeField] private int index;
    public Button smileButton;

    private void Awake()
    {
        smileButton = GetComponent<Button>();
    }
    private void Start()
    {
        smileButton.onClick.AddListener(SendSmile);
    }

    private void SendSmile()
    {
        Debug.Log("Вызвали метод SendSmile. Инвок OnSendSmile сработал");
        EventManager.Trigger(new LocalSmileSentEvent(index));
        EventManager.Trigger(new OnDisableSmileButtonsEvent());
    }


    private void OnDestroy()
    {
        smileButton.onClick.RemoveAllListeners();
    }
}
