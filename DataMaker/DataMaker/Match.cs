using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictingSoccer
{
    class Match
    {
        public string homeTeam;
        public string awayTeam;
        public int id;
        public int stage;
        public int homeTeamId;
        public int awayTeamId;
        public int homeTeamGoalsScored;
        public int awayTeamGoalsScored;
        public string dateString;
        public DateTime date;

        public void MakeDate()
        {
            string[] t = dateString.Split(' ');
            string[] s = t[0].Split('-');
            date = new DateTime(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
        }
    }

}
