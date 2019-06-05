using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATPDataMaker
{
    class MutualMatch : IMatch
    {
        public int player1ID;
        public int player2ID;

        public int player1Won;
        public double gameDiffPerSet;

        public MutualMatch(string home, string away, string homeSets, string awaySets)
        {
            player1ID = int.Parse(home);
            player2ID = int.Parse(away);

            CalculateResults(homeSets,awaySets);
        }

        private void CalculateResults(string homeSets, string awaySets)
        {
            var wSets = homeSets.Split('|');
            var lSets = awaySets.Split('|');
            int sets = 0;
            int diff = 0;

            for (int i = 0; i < wSets.Length; i++)
            {
                int w = int.Parse(wSets[i]);
                int l = int.Parse(lSets[i]);
                if (w > l)
                {
                    sets++;
                }
                diff += w - l;
            }
            player1Won = (sets > wSets.Length / 2) ? 1 : 0;
            gameDiffPerSet = diff / wSets.Length;
        }
    }
}
