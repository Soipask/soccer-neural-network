using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictingSoccer
{
    class Team
    {
        public string shortName;
        public int id;
        public int goalsFor;
        public int goalsAgainst;
        public int points;
        public int played;
        public int wins;
        public int draws;
        public int loses;
        public int homewins;
        public int homedraws;
        public int homeloses;
        public int homeGoalsFor;
        public int homeGoalsAgainst;
        public MatchForm[] currentForm = new MatchForm[10];

        public double longtimeStrength;
        public int seasonsIn;

        private byte maximumFormGamesStored;

        public Team(string name, int id, int gf, int ga, int p, byte maxFormStored)
        {
            shortName = name;
            this.id = id;
            goalsFor = gf;
            goalsAgainst = ga;
            points = p;
            maximumFormGamesStored = maxFormStored;
        }

        public Team(string name, int id, byte maxFormStored)
        {
            shortName = name;
            this.id = id;
            goalsFor = 0;
            goalsAgainst = 0;
            points = 0;
            played = 0;
            maximumFormGamesStored = maxFormStored;
        }

        public override string ToString()
        {
            string str = shortName + "\t" +
                played + "\t" +
                goalsFor + "\t" +
                goalsAgainst + "\t" +
                (goalsFor - goalsAgainst) + "\t" +
                points + "\t";

            for (int i = 0; i < currentForm.Length; i++)
            {
                if (currentForm[i] == null) break;
                str += currentForm[i];
            }
            return str;
        }
    }
}
