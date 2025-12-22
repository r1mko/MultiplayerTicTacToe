public class SkillCooldownManager
{
    // === Слайд ===
    private int slideCooldownTurnIndex = -1;
    private bool isSlideOnCooldown = false;

    // === Выстрел ===
    private int shotCooldownTurnIndex = -1;
    private bool isShotOnCooldown = false;

    // === Shuffle ===
    private int shuffleCooldownTurnIndex = -1;
    private bool isShuffleOnCooldown = false;

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
        shotCooldownTurnIndex = -1;
        isShotOnCooldown = false;
        shuffleCooldownTurnIndex = -1;
        isShuffleOnCooldown = false;
    }

    // === Слайд: текст ===
    public string GetSlideButtonText(int currentTurnIndex)
    {
        if (!isSlideOnCooldown)
            return "Slide";

        int playerWhoUsedSlide = (slideCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = CountPlayerTurns(slideCooldownTurnIndex + 1, currentTurnIndex, playerWhoUsedSlide);
        int remaining = 3 - turnsByThatPlayer;

        return remaining switch
        {
            3 => "In 3 turns",
            2 => "In 2 turns",
            1 => "Next turn",
            _ => "Slide"
        };
    }

    // === Выстрел: текст ===
    public string GetShotButtonText(int currentTurnIndex)
    {
        if (!isShotOnCooldown)
            return "Shot";

        int playerWhoUsedShot = (shotCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = CountPlayerTurns(shotCooldownTurnIndex + 1, currentTurnIndex, playerWhoUsedShot);
        int remaining = 5 - turnsByThatPlayer;

        return remaining switch
        {
            5 => "In 5 turns",
            4 => "In 4 turns",
            3 => "In 3 turns",
            2 => "In 2 turns",
            1 => "Next turn",
            _ => "Shot"
        };
    }

    // === Shuffle: текст ===
    public string GetShuffleButtonText(int currentTurnIndex)
    {
        if (!isShuffleOnCooldown)
            return "Shuffle";

        int playerWhoUsedShuffle = (shuffleCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = CountPlayerTurns(shuffleCooldownTurnIndex + 1, currentTurnIndex, playerWhoUsedShuffle);
        int remaining = 7 - turnsByThatPlayer;

        return remaining switch
        {
            7 => "In 7 turns",
            6 => "In 6 turns",
            5 => "In 5 turns",
            4 => "In 4 turns",
            3 => "In 3 turns",
            2 => "In 2 turns",
            1 => "Next turn",
            _ => "Shuffle"
        };
    }

    private int CountPlayerTurns(int fromTurn, int toTurn, int playerId)
    {
        int count = 0;
        for (int turn = fromTurn; turn <= toTurn; turn++)
        {
            int playerOnTurn = (turn + startOffSet) % 2;
            if (playerOnTurn == playerId)
                count++;
        }
        return count;
    }

    // === Слайд: использование ===
    public void OnSlideUsed(int currentTurnIndex)
    {
        slideCooldownTurnIndex = currentTurnIndex;
        isSlideOnCooldown = true;
    }

    // === Выстрел: использование ===
    public void OnShotUsed(int currentTurnIndex)
    {
        shotCooldownTurnIndex = currentTurnIndex;
        isShotOnCooldown = true;
    }

    // === Shuffle: использование ===
    public void OnShuffleUsed(int currentTurnIndex)
    {
        shuffleCooldownTurnIndex = currentTurnIndex;
        isShuffleOnCooldown = true;
    }

    // === Слайд: проверка снятия ===
    public bool ShouldRemoveSlideCooldown(int currentTurnIndex, int currentPlayerID)
    {
        if (!isSlideOnCooldown) return false;
        int playerWhoUsedSlide = (slideCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = CountPlayerTurns(slideCooldownTurnIndex + 1, currentTurnIndex, playerWhoUsedSlide);
        return turnsByThatPlayer >= 3;
    }

    // === Выстрел: проверка снятия ===
    public bool ShouldRemoveShotCooldown(int currentTurnIndex, int currentPlayerID)
    {
        if (!isShotOnCooldown) return false;
        int playerWhoUsedShot = (shotCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = CountPlayerTurns(shotCooldownTurnIndex + 1, currentTurnIndex, playerWhoUsedShot);
        return turnsByThatPlayer >= 1;
    }

    // === Shuffle: проверка снятия ===
    public bool ShouldRemoveShuffleCooldown(int currentTurnIndex, int currentPlayerID)
    {
        if (!isShuffleOnCooldown) return false;
        int playerWhoUsedShuffle = (shuffleCooldownTurnIndex + startOffSet) % 2;
        int turnsByThatPlayer = CountPlayerTurns(shuffleCooldownTurnIndex + 1, currentTurnIndex, playerWhoUsedShuffle);
        return turnsByThatPlayer >= 7;
    }

    // === Сброс кулдаунов ===
    public void RemoveSlideCooldown() => isSlideOnCooldown = false;
    public void RemoveShotCooldown() => isShotOnCooldown = false;
    public void RemoveShuffleCooldown() => isShuffleOnCooldown = false;

    // === Геттеры ===
    public bool IsSlideOnCooldown() => isSlideOnCooldown;
    public bool IsShotOnCooldown() => isShotOnCooldown;
    public bool IsShuffleOnCooldown() => isShuffleOnCooldown;
}