using System.Collections;
using UnityEngine;

public class BotController : MonoBehaviour
{
    private Coroutine botTurnCoroutine;

    void Update()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            return;
        }

        if (!GameManager.Singletone.IsPlaying)
        {
            return;
        }

        if (GameManager.Singletone.IsOurTurn())
        {
            return;
        }

        if (botTurnCoroutine != null)
        {
            return;
        }
        botTurnCoroutine = StartCoroutine(BotTurn());

    }

    private IEnumerator BotTurn()
    {
        yield return new WaitForSeconds(Random.Range(1, 3));
        if (BoardManager.Singltone.TryGetEmptyCell(out Cell cell))
        {
            GameManager.Singletone.OnClick(cell.row, cell.coll);
        }
        botTurnCoroutine = null;
    }
}
