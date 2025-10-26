using System.Collections.Generic;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Match
{
    public class MatchState
    {
        // Round tracking
        public int CurrentRound { get; set; }
        public int PlayerWins { get; set; }
        public int AIWins { get; set; }
        public MatchPhase CurrentPhase { get; set; }

        // Draft pools
        public List<TroopCombination> BaseCombinations { get; set; }
        public List<TroopCombination> AIGeneratedCombinations { get; set; }

        // Current round state
        public List<TroopCombination> PlayerDraftOptions { get; set; }
        public List<TroopCombination> AIDraftOptions { get; set; }
        public TroopCombination PlayerSelectedCombo { get; set; }
        public TroopCombination AISelectedCombo { get; set; }

        // History
        public List<RoundResult> RoundHistory { get; set; }
        public List<TroopCombination> PlayerPickHistory { get; set; }
        public List<TroopCombination> AIPickHistory { get; set; }

        public MatchState()
        {
            CurrentRound = 0;
            PlayerWins = 0;
            AIWins = 0;
            CurrentPhase = MatchPhase.MatchStart;

            BaseCombinations = new List<TroopCombination>();
            AIGeneratedCombinations = new List<TroopCombination>();
            PlayerDraftOptions = new List<TroopCombination>();
            AIDraftOptions = new List<TroopCombination>();

            RoundHistory = new List<RoundResult>();
            PlayerPickHistory = new List<TroopCombination>();
            AIPickHistory = new List<TroopCombination>();
        }

        public bool IsMatchOver()
        {
            return PlayerWins >= 4 || AIWins >= 4;
        }

        public Team GetMatchWinner()
        {
            if (PlayerWins >= 4) return Team.Player;
            if (AIWins >= 4) return Team.AI;
            return Team.Player; // Default
        }

        public void AwardRoundWin(Team winner)
        {
            if (winner == Team.Player)
                PlayerWins++;
            else
                AIWins++;
        }

        public List<TroopCombination> GetFullDraftPool()
        {
            var pool = new List<TroopCombination>();
            pool.AddRange(BaseCombinations);
            pool.AddRange(AIGeneratedCombinations);
            return pool;
        }
    }
}
