using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATPDataMaker
{
    class Match : IMatch
    {
        public string tournament;
        public int pointsForTournament;
        public bool havePoints;
        public int year;
        public string surface;
        public string winnerName;
        public string loserName;
        public int round;
        public int id;
        public int winnerId;
        public int loserId;
        public int winnerSetsWon;
        public int loserSetsWon;
        public int[] winnerSets;
        public int[] loserSets;
        public int gameDiffPerSet;

        public Match(string[] info)
        {
            tournament = info[0];
            if (int.TryParse(info[1], out pointsForTournament)) havePoints = true; else havePoints = false;
            year = int.Parse(info[2]);
            surface = info[3];
            winnerName = info[4];
            loserName = info[5];
            round = int.Parse(info[6]);
            id = int.Parse(info[7]);
            if (!int.TryParse(info[8], out winnerId)) winnerId = -1;
            if (!int.TryParse(info[9], out loserId)) loserId = -1;
            winnerSetsWon = int.Parse(info[10]);
            loserSetsWon = int.Parse(info[11]);
            winnerSets = new int[winnerSetsWon + loserSetsWon];
            loserSets = new int[winnerSetsWon + loserSetsWon];

            var wSets = info[12].Split('|');
            var lSets = info[13].Split('|');

            gameDiffPerSet = 0;
            for (int i = 0; i < winnerSetsWon + loserSetsWon; i++)
            {
                winnerSets[i] = int.Parse(wSets[i]);
                loserSets[i] = int.Parse(lSets[i]);

                gameDiffPerSet += winnerSets[i] - loserSets[i];
            }
            gameDiffPerSet /= wSets.Length;
        }
    }
}
