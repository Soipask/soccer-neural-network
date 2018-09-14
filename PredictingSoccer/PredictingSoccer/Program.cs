using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Accord.IO;
using Accord.MachineLearning;
using Accord.Neuro;
using Accord.Neuro.Learning;

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
        public string date;
    }

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
        public MatchForm[] currentForm = new MatchForm[10];

        public Team(string name, int id, int gf, int ga, int p)
        {
            shortName = name;
            this.id = id;
            goalsFor = gf;
            goalsAgainst = ga;
            points = p;
        }

        public Team(string name, int id)
        {
            shortName = name;
            this.id = id;
            goalsFor = 0;
            goalsAgainst = 0;
            points = 0;
            played = 0;
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

    class MatchForm
    {
        public GameResult finalpoints;
        public Team team;
        public Team matchAgainst;
        public int score;

        public int goalsFor;
        public int goalsAgainst;

        public enum GameResult : byte { L, D, W=3 };

        //score: (points got in game)*(point difference when teams played + constant)
        //constant: number, such that win against weaker team counts for more than 0
        
        public void CalculateScore()
        {
            int pointsGot = 0;

            switch (finalpoints)
            {
                case GameResult.L: pointsGot = 0; break;
                case GameResult.D: pointsGot = 1; break;
                case GameResult.W: pointsGot = 3; break;
            }

            //matchAgainst.points - team.points + team.points (constant)
            //recent wins therefore make bigger score, in reality they have more weight on current form
            score = pointsGot * matchAgainst.points;
        }

        public override string ToString()
        {
            switch (finalpoints)
            {
                case GameResult.L: return "L";
                case GameResult.D: return "D";
                case GameResult.W: return "W";
                default: return "";
            }
        }
    }

    class NeuralNetwork
    {
        private List<Match> matches = new List<Match>();
        private CsvReader season;
        public Dictionary<int, Team> teams = new Dictionary<int, Team>();

        private ActivationNetwork network;
        private ISupervisedLearning teacher;

        public NeuralNetwork (CsvReader reader)
        {
            season = reader;
        }

        public void FillSeason()
        {
            string[] row;
            Match match;

            while (!season.EndOfStream)
            {
                row = season.ReadLine();

                match = new Match
                {
                    homeTeam = row[0],
                    awayTeam = row[1],
                    id = int.Parse(row[2]),
                    stage = int.Parse(row[3]),
                    homeTeamId = int.Parse(row[4]),
                    awayTeamId = int.Parse(row[5]),
                    homeTeamGoalsScored = int.Parse(row[6]),
                    awayTeamGoalsScored = int.Parse(row[7]),
                    date = row[8]
                };

                if (!teams.ContainsKey(match.homeTeamId))
                {
                    Team team = new Team(match.homeTeam, match.homeTeamId);
                    teams.Add(match.homeTeamId,team);
                }
                if(!teams.ContainsKey(match.awayTeamId))
                {
                    Team team = new Team(match.awayTeam, match.awayTeamId);
                    teams.Add(match.awayTeamId, team);
                }

                matches.Add(match);
            }
            season.Dispose();
        }

        public void MakeScoreNeuralNetwork()
        {
            IActivationFunction function = new BipolarSigmoidFunction();

            
            network = new ActivationNetwork(function, 46, new int[] { 25, 10, 2 });
            //46:   home/away - wins/draws/loses/goalsScored/goalsAgainst in season/current form
            //      home team - home wins/draws/loses/goalsScored/goalsAgainst as a home team, the same for away team
            //      some calculations with form:    Sum(points got in a game * (20 - opponent team table position)
            //                                      Sum(points got in a game * (point difference right now)
            //                                      Sum(points got in a game * (point difference when game was played)
            //      + last 5 games between those two teams: thisDayHomeTeam wins/loses/draws/goalsScored/goalsAgainst
            //      + last 3 games between those two teams (home team plays home): home wins/loses/draws/goalsScored/goalsAgainst


            teacher = new ResilientBackpropagationLearning(network);


        }

        public void MakeWinNeuralNetwork()
        {
            IActivationFunction function = new BipolarSigmoidFunction();
            network = new ActivationNetwork(function, 46, new int[] { 25, 10, 1 });

            teacher = new ResilientBackpropagationLearning(network);

        }

        public void HalfSeasonTableFill()
        {
            Match match;
            for (int i = 0; i < matches.Count() / 2; i++) 
            {
                match = matches[i];

                AddPoints(match);
            }
        }

        private void AddPoints(Match match)
        {
            teams.TryGetValue(match.homeTeamId, out Team homeTeam);
            teams.TryGetValue(match.awayTeamId, out Team awayTeam);
            
            FillPoints(match, homeTeam, awayTeam, out int homePoints, out int awayPoints);
            
            homeTeam.goalsFor += match.homeTeamGoalsScored;
            homeTeam.goalsAgainst += match.awayTeamGoalsScored;
            homeTeam.played++;
            ChangeInForm(match, homeTeam, homePoints);
            
            awayTeam.goalsFor += match.awayTeamGoalsScored;
            awayTeam.goalsAgainst += match.homeTeamGoalsScored;
            awayTeam.played++;
            ChangeInForm(match, awayTeam, awayPoints);
        }
        private void GetPoints(Match match, out int homePoints, out int awayPoints)
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

        private void FillPoints(Match match, Team homeTeam, Team awayTeam, out int homePoints, out int awayPoints)
        {
            
            if (match.homeTeamGoalsScored > match.awayTeamGoalsScored)
            {

                homePoints = 3;
                awayPoints = 0;
                
                homeTeam.wins++;
                homeTeam.homewins++;
                awayTeam.loses++;
            }
            else if (match.awayTeamGoalsScored > match.homeTeamGoalsScored)
            {
                homePoints = 0;
                awayPoints = 3;

                homeTeam.loses++;
                awayTeam.wins++;
                homeTeam.homeloses++;
            }
            else
            {
                homePoints = awayPoints = 1;

                homeTeam.draws++;
                awayTeam.draws++;
                homeTeam.homedraws++;
            }

            homeTeam.points += homePoints;
            awayTeam.points += awayPoints;
        }
        private void ChangeInForm(Match match, Team team, int points)
        {
            MatchForm form = new MatchForm {
                finalpoints = (points < 1) ? MatchForm.GameResult.L :
                              (points == 1) ? MatchForm.GameResult.D : MatchForm.GameResult.W,
                team = team
            };

            Team matchAgainst;


            if (team.id == match.homeTeamId)
            {
                teams.TryGetValue(match.awayTeamId, out matchAgainst);
                form.goalsFor = match.homeTeamGoalsScored;
                form.goalsAgainst = match.awayTeamGoalsScored;
            }
            else
            {
                teams.TryGetValue(match.homeTeamId, out matchAgainst);
                form.goalsAgainst = match.homeTeamGoalsScored;
                form.goalsFor = match.awayTeamGoalsScored;
            }

            form.matchAgainst = matchAgainst;

            form.CalculateScore();

            for (int i = team.currentForm.Length - 1; i > 0; i--)
            {
                team.currentForm[i] = team.currentForm[i - 1];
            }

            team.currentForm[0] = form;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var season = new CsvReader(new StreamReader("../../../../Databases/england11.csv"), false, ',');

            NeuralNetwork neuralNetwork = new NeuralNetwork(season);
            neuralNetwork.FillSeason();
            neuralNetwork.HalfSeasonTableFill();

            List<Team> teams = new List<Team>();
            foreach(var m in neuralNetwork.teams)
            {
                teams.Add(m.Value);
            }
            var endOfSeason = teams.OrderByDescending(x => x.points).ThenByDescending(x => x.goalsFor - x.goalsAgainst);

            Console.WriteLine("TEA \t GP \t GF \t GA \t DIFF \t POI \t FORM");
            foreach (var team in endOfSeason)
            {
                Console.WriteLine(team);
            }

            neuralNetwork.MakeScoreNeuralNetwork();
        }
    }
}
