using System;
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
    class NeuralNetwork
    {
        private List<Match> matches = new List<Match>();

        private CsvReader season;

        private CsvReader previousSeasonsData;
        private List<string[]> previousSeasons;

        public Dictionary<int, Team> teams = new Dictionary<int, Team>();
        

        private ActivationNetwork network;
        private ISupervisedLearning teacher;

        private int maxMutualMatches = 5;

        public NeuralNetwork(CsvReader thisSeason, CsvReader previousSeasons)
        {
            season = thisSeason;
            previousSeasonsData = previousSeasons;
        }

        /// <summary>
        /// Fills list of matches, match by match, field by field
        /// </summary>
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
                    teams.Add(match.homeTeamId, team);
                }
                if (!teams.ContainsKey(match.awayTeamId))
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
            //      some calculations with form:    Sum(points got in a game * (20 - opponent team table position))
            //                                      Sum(points got in a game * (point difference right now))
            //                                      Sum(points got in a game * (point difference when game was played))
            //      + last 5 games between those two teams: thisDayHomeTeam wins/loses/draws/goalsScored/goalsAgainst
            //      + last 3 games between those two teams (home team plays home): home wins/loses/draws/goalsScored/goalsAgainst


            teacher = new ResilientBackpropagationLearning(network);


            //Just testing until the end of method
            previousSeasons = previousSeasonsData.Where(match => 
                (teams.ContainsKey(int.Parse(match[4])) || teams.ContainsKey(int.Parse(match[5])))).ToList();

            previousSeasonsData.Close();

            var x = FindLastMutualGames(teams.Keys.First(), teams.Keys.Last());
            var z = FindLastMutualGames(teams.Keys.First(), teams.Keys.Last());

            var y = FindLastMutualHomeGames(teams.Keys.First(), teams.Keys.Last());
        }

        public void MakeWinNeuralNetwork()
        {
            IActivationFunction function = new BipolarSigmoidFunction();
            network = new ActivationNetwork(function, 46, new int[] { 25, 10, 1 });

            teacher = new ResilientBackpropagationLearning(network);

        }

        /// <summary>
        /// Finds last games between those two teams regardless of who was playing at home from previous seasons data.
        /// </summary>
        /// <param name="homeTeamId"></param>
        /// <param name="awayTeamId"></param>
        /// <returns></returns>
        public MutualMatch[] FindLastMutualGames(int homeTeamId, int awayTeamId)
        {

            var x = previousSeasons.Where(l => 
                (l[5] == homeTeamId.ToString() && l[4] == awayTeamId.ToString())|| 
                (l[5] == awayTeamId.ToString() && l[4] == homeTeamId.ToString()));

            return FillMutualMatches(x, homeTeamId, awayTeamId); ;
        }

        /// <summary>
        /// Finds last games between those two teams where home team was playing at home.
        /// </summary>
        /// <param name="homeTeamId"></param>
        /// <param name="awayTeamId"></param>
        /// <returns></returns>
        public MutualMatch[] FindLastMutualHomeGames(int homeTeamId, int awayTeamId)
        {
            var x = previousSeasons.Where(l => (l[4] == homeTeamId.ToString() && l[5] == awayTeamId.ToString()));

            return FillMutualMatches(x, homeTeamId, awayTeamId); ;
        }

        private MutualMatch[] FillMutualMatches(IEnumerable<string[]> query, int homeTeamId, int awayTeamId)
        {
            var mutual = new MutualMatch[maxMutualMatches];
            int i = 0;
            foreach (var b in query)
            {
                mutual[i] = new MutualMatch()
                {
                    homeTeamId = homeTeamId,
                    awayTeamId = awayTeamId,
                    homeGoalsScored = byte.Parse(b[6]),
                    awayGoalsScored = byte.Parse(b[7])
                };
                mutual[i].homePoints = (mutual[i].homeGoalsScored > mutual[i].awayGoalsScored) ? (byte)10 :
                                (mutual[i].awayGoalsScored > mutual[i].homeGoalsScored) ? (byte)0 : (byte)1;
                if (i == maxMutualMatches - 1) break;
                i++;
            }
            return mutual;
        }
        
        /// <summary>
        /// Fills the table with half a season of matches. Right now just to test counting.
        /// </summary>
        public void HalfSeasonTableFill()
        {
            Match match;
            for (int i = 0; i < matches.Count() / 2; i++)
            {
                match = matches[i];

                AddPoints(match);
            }
        }

        /// <summary>
        /// Adds points and other data from certain match to the teams data
        /// </summary>
        /// <param name="match"></param>
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

        /// <summary>
        /// Gets match, outputs points the home team got and the points the away team got from that match.
        /// </summary>
        /// <param name="match"></param>
        /// <param name="homePoints"></param>
        /// <param name="awayPoints"></param>
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

        /// <summary>
        /// Fills data from match for both teams, for home team also fill home result field
        /// </summary>
        /// <param name="match"></param>
        /// <param name="homeTeam"></param>
        /// <param name="awayTeam"></param>
        /// <param name="homePoints"></param>
        /// <param name="awayPoints"></param>
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

        /// <summary>
        /// Gets match, team and points that team got from the match and makes new instance of MatchForm, 
        /// which it adds to the begining of form field in team
        /// </summary>
        /// <param name="match"></param>
        /// <param name="team"></param>
        /// <param name="points"></param>
        private void ChangeInForm(Match match, Team team, int points)
        {
            MatchForm form = new MatchForm
            {
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

}
