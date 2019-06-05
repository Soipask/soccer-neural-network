using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATPDataMaker
{
    class MatchFormGame
    {
        public Player player;
        public Player opponent;

        // point -> 1 for win, 0 for lose (easier to sum than bools)
        public byte point;
        public int score;
        public double gameDiffPerSet;

        public MatchFormGame(Player plyr, Player opp, Match match)
        {
            player = plyr;
            opponent = opp;
            point = (plyr.id == match.winnerId) ? (byte)1 : (byte)0;

            // score reflects power of win, 
            // all players are ranked 1 - 100 in the beginning of current year
            score = (150 - opp.rank) * point;

            double a = 0;
            for (int i = 0; i < match.winnerSets.Length; i++)
            {
                a += match.winnerSets[i] - match.loserSets[i];
            }

            // getting point from {0,1} to {-1,1}
            // gameDiff is reversed if opponent won
            gameDiffPerSet = (a * (2 * point - 1)) / match.winnerSets.Length;
        }

        public override string ToString()
        {
            return (point > 0)? "W":"L" ;
        }
    }
}
