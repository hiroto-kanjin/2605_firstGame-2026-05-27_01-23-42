using Watermelon.BubbleMerge;

public static class MapLinker
{
    public static int MaxLevelReached => LevelController.MaxLevelReached;
    public static int AmountOfLevels => LevelController.Database.AmountOfGameLevels; // hk修正：休眠中のlevelsではなく、現役のgameLevelsを見るように変更
}