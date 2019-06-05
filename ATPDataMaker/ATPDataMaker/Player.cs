using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATPDataMaker
{
    class Player
    {
        public string name;
        public int id;
        public int rank;
        public int wins;
        public int loses;
        public int played;
        public int gameDiffSum;
        public MatchFormGame[] currentForm;
        public SurfaceInfo hard;
        public SurfaceInfo clay;
        public SurfaceInfo grass;

        private byte maxFormStored;

        public Player(string name, int id, byte maxForm = 10)
        {
            this.name = name;
            this.id = id;
            maxFormStored = maxForm;

            hard = new SurfaceInfo("Hard", this);
            clay = new SurfaceInfo("Clay", this);
            grass = new SurfaceInfo("Grass", this);

            currentForm = new MatchFormGame[maxFormStored];
        }

        public void EndSeason()
        {
            wins = 0;
            loses = 0;
            played = 0;
            gameDiffSum = 0;
            
            hard.ResetSurface();
            clay.ResetSurface();  
            grass.ResetSurface();
        }

        public double GameDiffPerSet { get { return (played == 0)? 0 : (double)gameDiffSum / (double)played; } }

        public override string ToString()
        {
            string ret = name + " ";
            if (played > 0) ret += ((double)wins / (double)played).ToString() + " ";
            for (int i = 0; i < maxFormStored; i++)
            {
                ret += currentForm[i].ToString();
            }
            return ret;
        }
    }
}
