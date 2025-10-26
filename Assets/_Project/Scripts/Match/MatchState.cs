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

        // Draft pools (now supports both TroopCombination and RuntimeTroopCombination)
        public List<ICombination> BaseCombinations { get; set; }
        public List<ICombination> AIGeneratedCombinations { get; set; }

        // Current round state
        public List<ICombination> PlayerDraftOptions { get; set; }
        public List<ICombination> AIDraftOptions { get; set; }
        public ICombination PlayerSelectedCombo { get; set; }
        public ICombination AISelectedCombo { get; set; }

        // History
        public List<RoundResult> RoundHistory { get; set; }
        public List<ICombination> PlayerPickHistory { get; set; }
        public List<ICombination> AIPickHistory { get; set; }

        public MatchState()
        {
            CurrentRound = 0;
            PlayerWins = 0;
            AIWins = 0;
            CurrentPhase = MatchPhase.MatchStart;

            BaseCombinations = new List<ICombination>();
            AIGeneratedCombinations = new List<ICombination>();
            PlayerDraftOptions = new List<ICombination>();
            AIDraftOptions = new List<ICombination>();

            RoundHistory = new List<RoundResult>();
            PlayerPickHistory = new List<ICombination>();
            AIPickHistory = new List<ICombination>();
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

        public List<ICombination> GetFullDraftPool()
        {
            var pool = new List<ICombination>();
            pool.AddRange(BaseCombinations);
            pool.AddRange(AIGeneratedCombinations);
            return pool;
        }
    }
}
