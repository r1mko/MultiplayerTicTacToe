using System.Collections;
using UnityEngine;

public class BotController : MonoBehaviour
{
    private Coroutine botTurnCoroutine;

    void Update()
    {
        if (!GameManager.Singltone.IsPlaying)
        {
            return;
        }

        if (GameManager.Singltone.IsOurTurn())
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
            GameManager.Singltone.OnClick(cell.row, cell.coll);
        }
        botTurnCoroutine = null;
    }
}
