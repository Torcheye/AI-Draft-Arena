namespace AdaptiveDraftArena.Match
{
    public class RoundResult
    {
        public int RoundNumber { get; set; }
        public Team Winner { get; set; }
        public int PlayerHP { get; set; }
        public int AIHP { get; set; }
        public bool TimerExpired { get; set; }
        public float BattleDuration { get; set; }
    }
}
