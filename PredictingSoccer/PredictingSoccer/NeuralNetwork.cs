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

        public List<Team> table = new List<Team>();

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
                    table.Add(team);
                }
                if (!teams.ContainsKey(match.awayTeamId))
                {
                    Team team = new Team(match.awayTeam, match.awayTeamId);
                    teams.Add(match.awayTeamId, team);
                    table.Add(team);
                }

                matches.Add(match);
            }
            season.Dispose();
            
            previousSeasons = previousSeasonsData.Where(m =>
                (teams.ContainsKey(int.Parse(m[4])) || teams.ContainsKey(int.Parse(m[5])))).ToList();
            previousSeasonsData.Close();
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

            var data = new double[46];
            var result = new double[2];
            List<double[]> input = new List<double[]>();
            double[][] inputs = new double[matches.Count / 2 - teams.Count][];
            double[][] output = new double[matches.Count / 2 - teams.Count][];
            int i = 0;

            //Count at least 2 round, it probably won't help the network
            while (i < teams.Count) 
            {
                AddPoints(matches[i]);
                i++;
            }

            table.Sort((a, b) => b.points.CompareTo(a.points));

            Team homeTeam, awayTeam;
            
            while (i < matches.Count / 2)
            {
                FillInputData(matches[i], out data, out result);
                
                inputs[i - teams.Count] = data;

                AddPoints(matches[i]);
                
                output[i - teams.Count] = result;

                i++;
            }
        

            // Learning
            double error = double.PositiveInfinity;
            double previous;

            int epoch = 0;
            Console.WriteLine("\nStarting training");

            do
            {
                previous = error;
                epoch++;
                if (epoch % 10 == 0)
                {
                    Console.Write("Epoch: " + epoch + "\t");
                    Console.WriteLine("Error: " + error);
                }

                error = teacher.RunEpoch(inputs, output);

            } while (epoch < 10);

            Console.Write("Epoch: " + epoch + "\t");
            Console.WriteLine("Error: " + error);

            // Testing network success
            inputs = new double[matches.Count / 2][];
            var realResults = new double[matches.Count / 2][];
            while (i < matches.Count)
            {
                FillInputData(matches[i], out data, out result);
                
                inputs[i - matches.Count/2] = data;

                AddPoints(matches[i]);

                realResults[i - matches.Count/2] = result;

                i++;
            }

            var guess = new double[inputs.Length][];
            var intGuess = new int[inputs.Length][];
            int bothScores, homeScore, awayScore, team;
            bothScores = homeScore = awayScore = team = 0;

            
            Console.WriteLine("Real Results \t Won \t Guessed result \t Team guessed");
            for (int n = 0; n < inputs.Length; n++)
            {
                guess[n] = network.Compute(inputs[n]);
                intGuess[n] = new int[2];
                intGuess[n][0] = (int)Math.Round(guess[n][0]);
                intGuess[n][1] = (int)Math.Round(guess[n][1]);
                char wonby = (realResults[n][0] > realResults[n][1]) ? 'H' : (realResults[n][1] > realResults[n][0]) ? 'A' : 'D';
                char guessfor = (intGuess[n][0] > intGuess[n][1]) ? 'H' : (intGuess[n][1] > intGuess[n][0]) ? 'A' : 'D';
                Console.WriteLine($"{realResults[n][0]}:{realResults[n][1]}\tWon by {wonby}\t{intGuess[n][0]}:{intGuess[n][1]}\t {guessfor} was guessed");

                if (realResults[n][0] == intGuess[n][0]) homeScore++;
                if (realResults[n][1] == intGuess[n][1]) awayScore++;
                if (realResults[n][0] == intGuess[n][0] && realResults[n][1] == intGuess[n][1]) bothScores++;
                if (wonby == guessfor) team++;
            }
            // Always guesses 1:1...

            Console.WriteLine();
            Console.WriteLine("Results:");
            Console.WriteLine($"Home team goals for guessed right: {homeScore}, that's {homeScore * 100 / (double)inputs.Length}%");
            Console.WriteLine($"Away team goals for guessed right: {awayScore}, that's {awayScore * 100 / (double)inputs.Length}%");
            Console.WriteLine($"Exact result guessed right: {bothScores}, that's {bothScores * 100 / (double)inputs.Length}%");
            Console.WriteLine($"Team that won guessed right: {team}, that's {team * 100 / (double)inputs.Length}%");
        }

        public void MakeWinNeuralNetwork()
        {
            IActivationFunction function = new BipolarSigmoidFunction();
            network = new ActivationNetwork(function, 46, new int[] { 25, 10, 1 });

            teacher = new ResilientBackpropagationLearning(network);

        }

        private void FillInputData(Match match, out double[] data, out double[] result)
        {

            data = new double[46];
            result = new double[2];

            teams.TryGetValue(match.homeTeamId, out Team homeTeam);
            teams.TryGetValue(match.awayTeamId, out Team awayTeam);

            data[0] = homeTeam.wins;
            data[1] = homeTeam.draws;
            data[2] = homeTeam.loses;
            data[3] = homeTeam.goalsFor / (double)homeTeam.played;
            data[4] = homeTeam.goalsAgainst / (double)homeTeam.played;

            data[5] = awayTeam.wins;
            data[6] = awayTeam.draws;
            data[7] = awayTeam.loses;
            data[8] = awayTeam.goalsFor / (double)awayTeam.played;
            data[9] = awayTeam.goalsAgainst / (double)awayTeam.played;

            data[10] = homeTeam.homewins;
            data[11] = homeTeam.homedraws;
            data[12] = homeTeam.homeloses;
            data[13] = homeTeam.homeGoalsFor;
            data[14] = homeTeam.homeGoalsAgainst / (double)(data[10] + data[11] + data[12]);
            if (data[14] is double.NaN) data[14] = 0;

            data[15] = awayTeam.wins - awayTeam.homewins;
            data[16] = awayTeam.draws - awayTeam.homedraws;
            data[17] = awayTeam.loses - awayTeam.homeloses;
            data[18] = awayTeam.goalsFor - awayTeam.homeGoalsFor;
            data[19] = (awayTeam.goalsAgainst - awayTeam.homeGoalsAgainst) / (double)(data[15] + data[16] + data[17]);
            if (data[19] is double.NaN) data[19] = 0;

            GetForm(homeTeam, out var homeFormInput);
            homeFormInput.CopyTo(data, 20);

            GetForm(awayTeam, out var awayFormInput);
            awayFormInput.CopyTo(data, 27);

            GetMutualGames(homeTeam, awayTeam, out var mutualInput);

            mutualInput.CopyTo(data, 34);
            
            result[0] = match.homeTeamGoalsScored;
            result[1] = match.awayTeamGoalsScored;
        }

        private void GetForm(Team team, out double[] formInput)
        {
            formInput = new double[7];

            int pointsGot = 0;
            int i = 0;
            while (i < team.currentForm.Length) 
            {
                if (team.currentForm[i] == null) break;
                switch(team.currentForm[i].finalpoints)
                {
                    case MatchForm.GameResult.L: formInput[2]++; break;
                    case MatchForm.GameResult.D: formInput[1]++; pointsGot = 1; break;
                    case MatchForm.GameResult.W: formInput[0]++; pointsGot = 3; break;
                }
                formInput[3] += team.currentForm[i].goalsFor;
                formInput[4] += team.currentForm[i].goalsAgainst;
                formInput[5] += team.currentForm[i].score;
                formInput[6] += pointsGot * team.currentForm[i].matchAgainst.points;
                i++;
            }
            if (i > 0) 
            {
                formInput[3] /= i;
                formInput[4] /= i;
            }
        }

        private void GetMutualGames(Team homeTeam, Team awayTeam, out double[] mutualInput)
        {
            var mutual = FindLastMutualGames(homeTeam.id, awayTeam.id);

            mutualInput = new double[10];

            int i = 0;
            while (i < mutual.Length) 
            {
                if (mutual[i] == null) break;
                
                switch(mutual[i].homePoints)
                {
                    case 3: mutualInput[0]++; break;
                    case 1: mutualInput[1]++; break;
                    case 0: mutualInput[2]++; break;
                }
            
                mutualInput[3] += mutual[i].homeGoalsScored;
                mutualInput[4] += mutual[i].awayGoalsScored;

                i++;
            }
            if (i == 0) return;

            mutualInput[3] /= i;
            mutualInput[4] /= i;

            mutual = FindLastMutualHomeGames(homeTeam.id, awayTeam.id);
            i = 0;
            while (i < mutual.Length) 
            {
                if (mutual[i] == null) break;

                switch (mutual[i].homePoints)
                {
                    case 3: mutualInput[5]++; break;
                    case 1: mutualInput[6]++; break;
                    case 0: mutualInput[7]++; break;
                }

                mutualInput[8] += mutual[i].homeGoalsScored;
                mutualInput[9] += mutual[i].awayGoalsScored;

                i++;
            }
            if (i == 0) return;

            mutualInput[8] /= i;
            mutualInput[9] /= i;

        }

        /// <summary>
        /// Finds last games between those two teams regardless of who was playing at home from previous seasons data.
        /// </summary>
        /// <param name="homeTeamId"></param>
        /// <param name="awayTeamId"></param>
        /// <returns></returns>
        public MutualMatch[] FindLastMutualGames(int homeTeamId, int awayTeamId)
        {

            var suitableMatches = previousSeasons.Where(match => 
                (match[5] == homeTeamId.ToString() && match[4] == awayTeamId.ToString())|| 
                (match[5] == awayTeamId.ToString() && match[4] == homeTeamId.ToString()));

            return FillMutualMatches(suitableMatches, homeTeamId, awayTeamId); ;
        }

        /// <summary>
        /// Finds last games between those two teams where home team was playing at home.
        /// </summary>
        /// <param name="homeTeamId"></param>
        /// <param name="awayTeamId"></param>
        /// <returns></returns>
        public MutualMatch[] FindLastMutualHomeGames(int homeTeamId, int awayTeamId)
        {
            var suitable = previousSeasons.Where(match => 
                (match[4] == homeTeamId.ToString() && match[5] == awayTeamId.ToString()));

            return FillMutualMatches(suitable, homeTeamId, awayTeamId); ;
        }

        private MutualMatch[] FillMutualMatches(IEnumerable<string[]> suitableMatches, int homeTeamId, int awayTeamId)
        {
            var mutual = new MutualMatch[maxMutualMatches];
            int i = 0;
            foreach (var b in suitableMatches)
            {
                mutual[i] = new MutualMatch()
                {
                    homeTeamId = homeTeamId,
                    awayTeamId = awayTeamId,
                    homeGoalsScored = byte.Parse(b[6]),
                    awayGoalsScored = byte.Parse(b[7])
                };
                mutual[i].homePoints = (mutual[i].homeGoalsScored > mutual[i].awayGoalsScored) ? (byte)3 :
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
            homeTeam.homeGoalsFor += match.homeTeamGoalsScored;
            homeTeam.homeGoalsAgainst += match.awayTeamGoalsScored;
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
