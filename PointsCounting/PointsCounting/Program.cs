using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;

namespace PointsCounting
{
    [DelimitedRecord(",")]
    class Match
    {
        public string homeTeam;
        public string awayTeam;
        public int id;
        public int homeTeamId;
        public int awayTeamId;
        public int homeTeamGoalsScored;
        public int awayTeamGoalsScored;
    }
    class Program
    {
        static void Main(string[] args)
        {
            var engine = new FileHelperEngine<Match>();

            var result = engine.ReadFile("../../../../Databases/england08.csv");
        }


    }
}
