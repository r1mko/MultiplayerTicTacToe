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
    /// Проверяет, можно ли сейчас использовать Slide
    /// </summary>
    public bool CanUseSlide(int currentTurnIndex, int currentPlayerID)
    {
        if (isSlideOnCooldown)
            return false;

        // Проверяем, что прошло минимум N ходов игры
        if (currentTurnIndex < minTurnToUseSlide)
            return false;

        return true;
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