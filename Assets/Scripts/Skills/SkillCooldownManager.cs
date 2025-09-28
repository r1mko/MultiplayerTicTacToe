using System;

public class SkillCooldownManager
{
    private int slideCooldownTurnIndex = -1;
    private bool isSlideOnCooldown = false;
    private int startOffSet;
    private int minTurnToUseSlide = 3;

    public void Initialize(int offset)
    {
        startOffSet = offset;
        Reset();
    }

    public void Reset()
    {
        slideCooldownTurnIndex = -1;
        isSlideOnCooldown = false;
    }


    /// <summary>
    /// Возвращает текст для отображения на кнопке Slide: либо "Slide", либо "3/3"
    /// </summary>
    public string GetSlideButtonText(int currentTurnIndex)
    {
        if (!isSlideOnCooldown)
            return "Slide";

        int playerWhoUsedSlide = (slideCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = 0;

        for (int turn = slideCooldownTurnIndex + 1; turn <= currentTurnIndex; turn++)
        {
            int playerOnTurn = (turn + startOffSet) % 2;
            if (playerOnTurn == playerWhoUsedSlide)
            {
                turnsByThatPlayer++;
            }
        }

        int remaining = 3 - turnsByThatPlayer;

        return remaining switch
        {
            3 => "In 3 turns",
            2 => "In 2 turns",
            1 => "Next turn",
            _ => "Slide"
        };
    }

    /// <summary>
    /// Вызывается при использовании Slide
    /// </summary>
    public void OnSlideUsed(int currentTurnIndex)
    {
        slideCooldownTurnIndex = currentTurnIndex;
        isSlideOnCooldown = true;
    }

    /// <summary>
    /// Проверяет, пора ли снимать кулдаун (прошло 3 хода игрока, который использовал Slide)
    /// </summary>
    public bool ShouldRemoveCooldown(int currentTurnIndex, int currentPlayerID)
    {
        if (!isSlideOnCooldown)
            return false;

        int playerWhoUsedSlide = (slideCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = 0;

        for (int turn = slideCooldownTurnIndex + 1; turn <= currentTurnIndex; turn++)
        {
            int playerOnTurn = (turn + startOffSet) % 2;
            if (playerOnTurn == playerWhoUsedSlide)
            {
                turnsByThatPlayer++;
            }
        }

        return turnsByThatPlayer >= 3;
    }

    /// <summary>
    /// Снимает кулдаун (вызывается после проверки)
    /// </summary>
    public void RemoveCooldown()
    {
        isSlideOnCooldown = false;
    }

    public bool IsOnCooldown() => isSlideOnCooldown;
}