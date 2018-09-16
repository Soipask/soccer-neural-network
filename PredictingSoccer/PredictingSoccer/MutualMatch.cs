using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictingSoccer
{
    /// <summary>
    /// Term home here stands for home team in predicted game
    /// </summary>
    class MutualMatch
    {
        public int homeTeamId;
        public int awayTeamId;
        
        // 3 points for the win, 1 point for draw - easy and quick figuring number of wins and loses in one field
        public byte homePoints;

        public byte homeGoalsScored;
        public byte awayGoalsScored;
    }
}
