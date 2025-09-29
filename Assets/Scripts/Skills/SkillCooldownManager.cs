using System;

public class SkillCooldownManager
{
    private int slideCooldownTurnIndex = -1;
    private bool isSlideOnCooldown = false;
    private int startOffSet;

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

    public string GetSlideButtonText(int currentTurnIndex)
    {
        if (!isSlideOnCooldown)
            return "Slide";

        int raw = (slideCooldownTurnIndex + startOffSet) % 2;
        int playerWhoUsedSlide = raw < 0 ? raw + 2 : raw;

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

    public void OnSlideUsed(int currentTurnIndex)
    {
        slideCooldownTurnIndex = currentTurnIndex;
        isSlideOnCooldown = true;
    }

    public bool ShouldRemoveCooldown(int currentTurnIndex, int currentPlayerID)
    {
        if (!isSlideOnCooldown)
            return false;

        int raw = (slideCooldownTurnIndex + startOffSet) % 2;
        int playerWhoUsedSlide = raw < 0 ? raw + 2 : raw;

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

    public void RemoveCooldown()
    {
        isSlideOnCooldown = false;
    }

    public bool IsOnCooldown() => isSlideOnCooldown;
}