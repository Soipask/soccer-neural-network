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

        private List<string[]> season;
        
        private List<string[]> previousSeasons;

        public Dictionary<int, Team> teams = new Dictionary<int, Team>();

        public List<Team> table = new List<Team>();

        private ActivationNetwork network;
        private ISupervisedLearning teacher;

        private int maxMutualMatches = 5;
        private byte maxFormMatches = 10;

        public double accuracy;
        public double sumBetting = 0;
        public double sumRealBetting = 0;

        private int inputNeurons = 46;
        private int outputNeurons = 3;

        private int teamsInOneSeason = 20;

        public NeuralNetwork(CsvReader data)
        {
            var dataList = data.ToList();

            season = dataList.TakeWhile(x => x[9] == dataList[0][9]).Reverse().ToList();

            previousSeasons = dataList.SkipWhile(x => x[9] == dataList[0][9]).ToList();
        }

        //-------------------------------------------------------------------
        /// <summary>
        /// Makes neural network, then makes teacher for it, which teaches it one and a half season worth of data.
        /// Then compares guesses from it for the other half season with real results and writes statistics.
        /// </summary>
        public void MakeScoreNeuralNetwork()
        {
            IActivationFunction function = new BipolarSigmoidFunction();


            network = new ActivationNetwork(function, inputNeurons, new int[] { 25, 15, outputNeurons });

            //46:   home/away - wins/draws/loses/goalsScored/goalsAgainst in season/current form
            //      home team - home wins/draws/loses/goalsScored/goalsAgainst as a home team, the same for away team
            //      some calculations with form:    Sum(points got in a game * (point difference right now))
            //                                      Sum(points got in a game * (point difference when game was played))
            //      + last 5 games between those two teams: thisDayHomeTeam wins/loses/draws/goalsScored/goalsAgainst
            //      + last 3 games between those two teams (home team plays home): home wins/loses/draws/goalsScored/goalsAgainst
            //      + long-time strenght of teams (average number of points got in seasons before)


            teacher = new ResilientBackpropagationLearning(network);

            var data = new double[46];
            var result = new double[outputNeurons];

            var input = new List<double[]>();
            var output = new List<double[]>();

            int i = 0;

            FillTeamsLongTimeStrength();

            ResetSeasonProgress();

            var lastSeason = OneMoreSeasonAdd(matches[0].date.Year - 1, out var lastSeasonResults);

            ResetSeasonProgress();


            // Count at least 2 round, it probably won't help the network
            while (i < teamsInOneSeason)
            {
                AddPoints(matches[i], teams);
                i++;
            }

            int back = i;

            // Fills data for neural network and updates table (teams' data)
            while (i < matches.Count / 2)
            {
                FillInputData(matches[i], previousSeasons, teams, out data, out result);

                input.Add(data);

                AddPoints(matches[i], teams);

                output.Add(result);

                i++;
            }


            // Learning
            double error = double.PositiveInfinity;
            double previous;

            int epoch = 0;
            //Console.WriteLine("\nStarting training");

            var inputsArray = new double[input.Count + lastSeason.Length][];
            var outputsArray = new double[output.Count + lastSeasonResults.Length][];

            input.ToArray().CopyTo(inputsArray, 0);
            lastSeason.CopyTo(inputsArray, input.Count);

            output.ToArray().CopyTo(outputsArray, 0);
            lastSeasonResults.CopyTo(outputsArray, output.Count);

            do
            {
                previous = error;
                epoch++;
                if (epoch % 100 == 0)
                {
                    Console.Write("Epoch: " + epoch + "\t");
                    Console.WriteLine("Error: " + error);
                }

                error = teacher.RunEpoch(inputsArray, outputsArray);

            } while (epoch < 3000);

            Console.Write("Epoch: " + epoch + "\t");
            Console.WriteLine("Error: " + error);

            // Testing network success
            back = i;
            input = new List<double[]>();
            output = new List<double[]>();

            while (i < matches.Count)
            {
                FillInputData(matches[i], previousSeasons, teams, out data, out result);

                input.Add(data);

                AddPoints(matches[i], teams);

                double[] resultPlusBet = new double[6];
                for (int j = 0; j < 3; j++)
                {
                    resultPlusBet[j] = result[j];
                    resultPlusBet[j + 3] = double.Parse(season[i][j + 10], System.Globalization.CultureInfo.InvariantCulture);
                }

                output.Add(resultPlusBet);

                i++;
            }

            var guess = new double[outputNeurons];
            int team;
            double betting;
            double realBetting;
            team = 0;

            Console.WriteLine();
            Console.WriteLine("Real Results:");
            for (int n = 0; n < input.Count; n++)
            {
                guess = network.Compute(input[n]);

                char wonby = (output[n][0] == 1) ? 'H' : (output[n][2] == 1) ? 'A' : 'D';

                char guessfor = (guess[0] >= guess[1] && guess[0] >= guess[2]) ? 'H' : (guess[2] >= guess[1] && guess[2] >= guess[0]) ? 'A' : 'D';

                betting = -1;
                realBetting = -1;

                if (wonby == guessfor)
                {
                    team++;
                    switch (wonby)
                    {
                        case 'H': betting = output[n][3]; break;
                        case 'D': betting = output[n][4]; break;
                        case 'A': betting = output[n][5]; break;
                    }
                    realBetting = 0.94 * betting;
                }

                Console.WriteLine($"Won by {wonby} and {guessfor} was guessed, if you bet 100Kč, you would get {betting * 100} or {realBetting * 100} if you count 6% fee.");

                sumBetting += betting;
                sumRealBetting += realBetting;
            }

            accuracy = team / (double)input.Count;

            Console.WriteLine("Results:");
            Console.WriteLine($"Team that won guessed right: {team}, that's {accuracy * 100}%");
            Console.WriteLine($"If you bet on this network results, one game at a time, you would win {sumBetting * 100} or {sumRealBetting * 100} if you count 6% fees.");
            Console.WriteLine();
        }
        //-------------------------------------------------------------------



        /// <summary>
        /// Fills list of matches, match by match, field by field
        /// </summary>
        public void FillSeason()
        {

            Match match;

            foreach(var row in season)
            {
                match = FillAMatch(row, table, teams);
                matches.Add(match);
            }
        }

        public void FillSeason(IEnumerable<string[]> stringOfMatches,List<Match> matches, List<Team> table, Dictionary<int, Team> teams)
        {
            foreach(var row in stringOfMatches)
            {
                var match = FillAMatch(row, table,teams);
                matches.Add(match);
            }
        }

        private Match FillAMatch(string[] row, List<Team> table, Dictionary<int, Team> teams)
        {
            var match = new Match
            {
                homeTeam = row[0],
                awayTeam = row[1],
                id = int.Parse(row[2]),
                stage = int.Parse(row[3]),
                homeTeamId = int.Parse(row[4]),
                awayTeamId = int.Parse(row[5]),
                homeTeamGoalsScored = int.Parse(row[6]),
                awayTeamGoalsScored = int.Parse(row[7]),
                dateString = row[8]
            };

            match.MakeDate();

            if (!teams.ContainsKey(match.homeTeamId))
            {
                Team team = new Team(match.homeTeam, match.homeTeamId, maxFormMatches);
                teams.Add(match.homeTeamId, team);
                table.Add(team);
            }
            if (!teams.ContainsKey(match.awayTeamId))
            {
                Team team = new Team(match.awayTeam, match.awayTeamId, maxFormMatches);
                teams.Add(match.awayTeamId, team);
                table.Add(team);
            }

            return match;
        }

        private void FillTeamsLongTimeStrength()
        {
            int firstSeason = 0;
            int lastSeason = 0;

            for (int i = 0; i < 4; i++)
            {
                lastSeason = lastSeason * 10 + previousSeasons[0][9][i] - 48;
                firstSeason = firstSeason * 10 + previousSeasons[previousSeasons.Count - 1][9][i] - 48;
            }

            for (int i = lastSeason; i > firstSeason - 1; i--)
                //last season doesn't need to be counted, because it is evaluated for data to learn 
                //and then will be calculated separately
            {
                FillTeamsPoints(i);
            }
        }

        /// <summary>
        /// Resets every column for every team except longtimeStrenght, which will be adjusted accordingly
        /// </summary>
        private void ResetSeasonProgress()
        {
            foreach(var pair in teams)
            {
                var team = pair.Value;

                team.ResetData();
            }
        }

        private void RecalculateLongTimeStrength()
        {

            foreach (var pair in teams)
            {
                var team = pair.Value;

                if (team.played == 0) continue;
                
                team.longtimeStrength = (team.seasonsIn * team.longtimeStrength + team.points) / (double)(team.seasonsIn + 1);
                team.seasonsIn++;
            }
        }

        private void FillTeamsPoints(int seasonStart)
        {
            Dictionary<int, int> values = new Dictionary<int, int>();
            int[] points = new int[20];
            List<Match> matchesList = new List<Match>();
            List<Team> teamList = new List<Team>();

            int i = 0;

            var season = previousSeasons
                .SkipWhile(x => x[9] != (seasonStart + "/" + (seasonStart + 1)))
                .TakeWhile(x => x[9] == (seasonStart + "/" + (seasonStart + 1)));

            FillSeason(season, matchesList, teamList, teams);

            foreach (var match in matchesList)
            {
                teams.TryGetValue(match.homeTeamId, out Team homeTeam);
                teams.TryGetValue(match.awayTeamId, out Team awayTeam);

                FillPoints(match, homeTeam, awayTeam, out int homePoints, out int awayPoints);

                if (match.stage == 1)
                {
                    homeTeam.seasonsIn++;
                    awayTeam.seasonsIn++;
                }
                if (values.TryGetValue(homeTeam.id, out int val))
                {
                    points[val] += homePoints;
                }
                else
                {
                    values.Add(homeTeam.id, i);
                    points[i] += homePoints;
                    i++;
                }
                if (values.TryGetValue(awayTeam.id, out val))
                {
                    points[val] += awayPoints;
                }
                else
                {
                    values.Add(awayTeam.id, i);
                    points[i] += awayPoints;
                    i++;
                }
            }

            foreach (var key in values.Keys)
            {
                values.TryGetValue(key, out int val);
                int point = points[val];
                teams.TryGetValue(key, out Team team);
                team.longtimeStrength = (team.longtimeStrength * (team.seasonsIn - 1) + point) / (double)team.seasonsIn;
            }
        }

        /// <summary>
        /// Gets season, takes this season and makes data from whole season for neural network to learn.
        /// </summary>
        /// <param name="season"></param>
        /// <param name="seasonResults"></param>
        /// <returns></returns>
        private double[][] OneMoreSeasonAdd(int season, out double[][] seasonResults)
        {
            double[][] wholeSeason = new double[360][];
            List<Match> thisSeason = new List<Match>();
            List<Team> seasonsTable = new List<Team>();

            List<double[]> input = new List<double[]>();
            List<double[]> output = new List<double[]>();
            double[] data;
            double[] result;

            var thisYearsTeams = new Dictionary<int, Team>();

            var matches = previousSeasons.SkipWhile(m => m[9] != (season.ToString() + "/" + (season + 1).ToString())).
                TakeWhile(m => m[9] == season.ToString() + "/" + (season + 1).ToString()).Reverse().ToList();

            FillSeason(matches, thisSeason, seasonsTable, teams);

            int i = 0;
            while (i < teamsInOneSeason) 
            {
                AddPoints(thisSeason[i], teams);
                i++;
            }

            var seasonsBefore = previousSeasons.SkipWhile(m => m[9] != season.ToString() + "/" + (season + 1).ToString()).
                SkipWhile(m => m[9] == season.ToString() + "/" + (season + 1).ToString()).ToList();

            while (i < thisSeason.Count)
            {
                FillInputData(thisSeason[i], seasonsBefore, teams, out data, out result);

                input.Add(data);

                AddPoints(thisSeason[i], teams);

                output.Add(result);

                i++;
            }

            seasonResults = output.ToArray();
            wholeSeason = input.ToArray();

            RecalculateLongTimeStrength();

            return wholeSeason;
        }
        

        /// <summary>
        /// Fills input data for neural network as well as results.
        /// </summary>
        /// <param name="match"></param>
        /// <param name="data"></param>
        /// <param name="result"></param>
        private void FillInputData(Match match, List<string[]> previousSeasons, Dictionary<int, Team> teams, out double[] data,  out double[] result)
        {

            data = new double[46];
            result = new double[outputNeurons];

            teams.TryGetValue(match.homeTeamId, out Team homeTeam);
            teams.TryGetValue(match.awayTeamId, out Team awayTeam);

            data[0] = homeTeam.wins;
            data[1] = homeTeam.draws;
            data[2] = homeTeam.loses;
            data[3] = homeTeam.goalsFor / (double)homeTeam.played;
            data[4] = homeTeam.goalsAgainst / (double)homeTeam.played;
            if (double.IsNaN(data[3])) data[3] = 0;
            if (double.IsNaN(data[4])) data[4] = 0;

            data[5] = awayTeam.wins;
            data[6] = awayTeam.draws;
            data[7] = awayTeam.loses;
            data[8] = awayTeam.goalsFor / (double)awayTeam.played;
            data[9] = awayTeam.goalsAgainst / (double)awayTeam.played;
            if (double.IsNaN(data[8])) data[8] = 0;
            if (double.IsNaN(data[9])) data[9] = 0;

            data[10] = homeTeam.homewins;
            data[11] = homeTeam.homedraws;
            data[12] = homeTeam.homeloses;
            data[13] = homeTeam.homeGoalsFor / (double)(data[10] + data[11] + data[12]);
            data[14] = homeTeam.homeGoalsAgainst / (double)(data[10] + data[11] + data[12]);
            if (data[14] is double.NaN) data[14] = 0;

            data[15] = awayTeam.wins - awayTeam.homewins;
            data[16] = awayTeam.draws - awayTeam.homedraws;
            data[17] = awayTeam.loses - awayTeam.homeloses;
            data[18] = (awayTeam.goalsFor - awayTeam.homeGoalsFor) / (double)(data[15] + data[16] + data[17]);
            data[19] = (awayTeam.goalsAgainst - awayTeam.homeGoalsAgainst) / (double)(data[15] + data[16] + data[17]);
            if (data[19] is double.NaN) data[19] = 0;

            GetForm(homeTeam, out var homeFormInput);
            homeFormInput.CopyTo(data, 20);

            GetForm(awayTeam, out var awayFormInput);
            awayFormInput.CopyTo(data, 27);

            GetMutualGames(homeTeam, awayTeam, previousSeasons, out var mutualInput);

            mutualInput.CopyTo(data, 34);

            //data[44] = homeTeam.longtimeStrength;
            //data[45] = awayTeam.longtimeStrength;
            
            if (match.homeTeamGoalsScored > match.awayTeamGoalsScored) result[0] = 1;
            else if (match.homeTeamGoalsScored < match.awayTeamGoalsScored) result[2] = 1;
            else result[1] = 1;
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

        private void GetMutualGames(Team homeTeam, Team awayTeam, List<string[]> previousSeasons, out double[] mutualInput)
        {
            var mutual = FindLastMutualGames(homeTeam.id, awayTeam.id, previousSeasons);

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

            mutual = FindLastMutualHomeGames(homeTeam.id, awayTeam.id, previousSeasons);
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
        public MutualMatch[] FindLastMutualGames(int homeTeamId, int awayTeamId, List<string[]> previousSeasons)
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
        public MutualMatch[] FindLastMutualHomeGames(int homeTeamId, int awayTeamId, List<string[]> previousSeasons)
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

                AddPoints(match, teams);
            }
        }

        /// <summary>
        /// Adds points and other data from certain match to the teams data
        /// </summary>
        /// <param name="match"></param>
        private void AddPoints(Match match, Dictionary<int, Team> teams)
        {
            teams.TryGetValue(match.homeTeamId, out Team homeTeam);
            teams.TryGetValue(match.awayTeamId, out Team awayTeam);

            FillPoints(match, homeTeam, awayTeam, out int homePoints, out int awayPoints);

            homeTeam.goalsFor += match.homeTeamGoalsScored;
            homeTeam.goalsAgainst += match.awayTeamGoalsScored;
            homeTeam.homeGoalsFor += match.homeTeamGoalsScored;
            homeTeam.homeGoalsAgainst += match.awayTeamGoalsScored;
            homeTeam.played++;
            ChangeInForm(match, homeTeam, homePoints, teams);

            awayTeam.goalsFor += match.awayTeamGoalsScored;
            awayTeam.goalsAgainst += match.homeTeamGoalsScored;
            awayTeam.played++;
            ChangeInForm(match, awayTeam, awayPoints, teams);
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
        private void ChangeInForm(Match match, Team team, int points, Dictionary<int, Team> teams)
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
