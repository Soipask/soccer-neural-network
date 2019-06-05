using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATPDataMaker
{
    class SurfaceInfo
    {
        public string name;
        private Player player;
        public MatchFormGame[] form;
        public int wins;
        public int loses;
        public int played;
        public int gameDiffSum;
        public double GameDiffPerSet { get { return (played == 0)? 0 : gameDiffSum / played; } }


        private int maxFormStored;

        public SurfaceInfo(string name, Player player, int maxForm = 10)
        {
            this.name = name;
            this.player = player;
            maxFormStored = maxForm;
            form = new MatchFormGame[maxForm];
        }

        public void UpdateSurface(Player player, Player opponent, Match match)
        {
            played++;
            if (player.id == match.winnerId)
            {
                wins++;
                gameDiffSum += match.gameDiffPerSet;
            }
            else
            {
                loses++;
                gameDiffSum -= match.gameDiffPerSet;
            }

            MatchFormGame formGame = new MatchFormGame(player, opponent, match);
            for (int i = maxFormStored - 1; i > 0; i--)
            {
                form[i] = form[i - 1];
            }

            form[0] = formGame;
        }

        public void ResetSurface()
        {
            wins = 0;
            loses = 0;
            played = 0;
            gameDiffSum = 0;
        }

        public override string ToString()
        {
            return (played == 0) ? 0.ToString() : ((double)wins / (double)played).ToString();
        }
    }
}
