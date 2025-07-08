using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SmileScreen: MonoBehaviour
{
    [SerializeField] private Transform spawnPlace;
    [SerializeField] List<SmileButton> smileButtonList;
    private Action OnDisableButtonsAction;

    public void Init(Action<int> action)
    {
        OnDisableButtonsAction += StartSmileCooldown;
        foreach (var item in smileButtonList)
        {
            item.Init(action, OnDisableButtonsAction);
        }
    }

    private void StartSmileCooldown()
    {
        StartCoroutine(SmileCooldown());
    }

    public Transform GetParentPlace()
    {
        return spawnPlace;
    }

    private void AllButtonDisable()
    {
        foreach (var item in smileButtonList)
        {
            item.smileButton.interactable = false;
        }
    }

    private void AllButtonEnable()
    {
        foreach (var item in smileButtonList)
        {
            item.smileButton.interactable = true;
        }
    }

    public IEnumerator SmileCooldown()
    {

        AllButtonDisable();
        yield return new WaitForSeconds(3f);
        AllButtonEnable();
    }

    private void OnDestroy()
    {
        OnDisableButtonsAction -= StartSmileCooldown;
    }
}
