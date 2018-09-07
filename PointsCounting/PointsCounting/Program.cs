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
        public int stage;
        public int homeTeamId;
        public int awayTeamId;
        public int homeTeamGoalsScored;
        public int awayTeamGoalsScored;
        public string date;
    }

    class Team
    {
        public string shortName;
        public int id;
        public int goalsFor;
        public int goalsAgainst;
        public int points;
        public MatchForm[] currentForm = new MatchForm[10];

        public Team(string name, int id, int gf, int ga, int p)
        {
            shortName = name;
            this.id = id;
            goalsFor = gf;
            goalsAgainst = ga;
            points = p;
        }

        public override string ToString()
        {
            string str = shortName + "\t" + points + "\t" + goalsFor + "\t" + goalsAgainst + "\t" + (goalsFor - goalsAgainst) + "\t";
            for (int i = 0; i < currentForm.Length; i++) 
            {
                if (currentForm[i] == null) break;
                str += currentForm[i];
            }
            return str;
        }
    }

    class MatchForm
    {
        public int finalpoints;
        public Team matchAgainst;

        public override string ToString()
        {
            switch (finalpoints)
            {
                case 0: return "L";
                case 1: return "D";
                case 3: return "W";
                default: return "";
            }
        }
    }

    class Program
    {
        static Team[] table;
        static Dictionary<int, Team> teamDictionary;

        private static void AddTeams(Match match, int i)
        {
            GetPoints(match, out int homePoints, out int awayPoints);

            Team homeTeam = new Team(match.homeTeam, match.homeTeamId, match.homeTeamGoalsScored, match.awayTeamGoalsScored, homePoints);
            Team awayTeam = new Team(match.awayTeam, match.awayTeamId, match.awayTeamGoalsScored, match.homeTeamGoalsScored, awayPoints);

            table[i] = homeTeam;
            table[i + 1] = awayTeam;

            teamDictionary.Add(match.homeTeamId,homeTeam);
            teamDictionary.Add(match.awayTeamId,awayTeam);

            ChangeInForm(match, homeTeam, homePoints);
            ChangeInForm(match, awayTeam, awayPoints);

        }
        private static void AddPoints(Match match)
        {
            teamDictionary.TryGetValue(match.homeTeamId, out Team homeTeam);
            teamDictionary.TryGetValue(match.awayTeamId, out Team awayTeam);

            GetPoints(match, out int homePoints, out int awayPoints);

            homeTeam.points += homePoints;
            homeTeam.goalsFor += match.homeTeamGoalsScored;
            homeTeam.goalsAgainst += match.awayTeamGoalsScored;
            ChangeInForm(match, homeTeam, homePoints);

            awayTeam.points += awayPoints;
            awayTeam.goalsFor += match.awayTeamGoalsScored;
            awayTeam.goalsAgainst += match.homeTeamGoalsScored;
            ChangeInForm(match, awayTeam, awayPoints);
        }
        private static void GetPoints(Match match,out int homePoints, out int awayPoints)
        {
            if (match.homeTeamGoalsScored > match.awayTeamGoalsScored)
            {
                homePoints = 3;
                awayPoints = 0;
            }
            else if (match.awayTeamGoalsScored > match.homeTeamGoalsScored)
            {
                homePoints = 0;
                awayPoints = 3;
            }
            else
            {
                homePoints = awayPoints = 1;
            }
        }
        private static void ChangeInForm(Match match, Team team, int points)
        {
            MatchForm form = new MatchForm { finalpoints = points };

            Team matchAgainst;

            if (team.id == match.homeTeamId)
            {
                teamDictionary.TryGetValue(match.awayTeamId, out matchAgainst);
            }
            else
            {
                teamDictionary.TryGetValue(match.homeTeamId, out matchAgainst);
            }

            for (int i = team.currentForm.Length - 1; i > 0; i--) 
            {
                team.currentForm[i] = team.currentForm[i - 1];
            }

            team.currentForm[0] = form;
        }

        static void Main(string[] args)
        {
            var engine = new FileHelperEngine<Match>();

            var season = engine.ReadFile("../../../../Databases/england08.csv");

            table = new Team[20];

            teamDictionary = new Dictionary<int, Team>();

            int i = 0;
            foreach (var match in season) 
            {
                if (match.stage == 1)
                {
                    AddTeams(match, i);
                    i = i + 2;
                }
                else
                {
                    AddPoints(match);   
                }
            }

            var endOfSeason = table.OrderByDescending(x => x.points).ThenByDescending(x => x.goalsFor - x.goalsAgainst);
            
            Console.WriteLine("TEA \t POI \t GF \t GA \t DIFF \t FORM");
            foreach(var team in endOfSeason)
            {
                Console.WriteLine(team);
            }
        }
    }
}
