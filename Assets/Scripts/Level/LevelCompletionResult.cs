namespace BeanBlaster.Level
{
    public readonly struct LevelCompletionResult
    {
        public readonly int levelIndex;
        public readonly float timeSeconds;
        public readonly int deaths;
        public readonly int kills;
        public readonly int collectedCoins;
        public readonly int awardedCoins;
        public readonly bool firstCompletion;
        public readonly bool noDeath;

        public LevelCompletionResult(int levelIndex, float timeSeconds, int deaths, int kills, int collectedCoins, int awardedCoins, bool firstCompletion, bool noDeath)
        {
            this.levelIndex = levelIndex;
            this.timeSeconds = timeSeconds;
            this.deaths = deaths;
            this.kills = kills;
            this.collectedCoins = collectedCoins;
            this.awardedCoins = awardedCoins;
            this.firstCompletion = firstCompletion;
            this.noDeath = noDeath;
        }
    }
}
