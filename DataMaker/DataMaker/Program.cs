using PredictingSoccer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


using Accord.IO;
using Accord.MachineLearning;

namespace DataMaker
{
    class Program
    {

        private List<Match> matches = new List<Match>();

        private List<string[]> season;

        private List<string[]> previousSeasons;

        public Dictionary<int, Team> teams = new Dictionary<int, Team>();

        public List<Team> table = new List<Team>();

        private int maxMutualMatches = 5;
        private byte maxFormMatches = 5;
        
        public double sumBetting = 0;
        public double sumRealBetting = 0;

        private int inputNeurons = 46;
        private int outputNeurons = 3;

        private int teamsInOneSeason = 20;

        private TextWriter log = Console.Out;
        private bool inputLogged = false;

        static void Main(string[] args)
        {
            string resource;
            string league;

            /**/
            resource = Properties.Resources.eng;
            league = "eng";
            /**/
            /*/
            resource = Properties.Resources.fra;
            league = "fra";
            /**/
            /*/
            resource = Properties.Resources.ger;
            league = "ger";
            /**/

            var data = new CsvReader(new StringReader(resource), false, ',');
            
            string version = "4";
            string mod = "dfswofs";

            var writer = new StreamWriter("..\\..\\..\\..\\Databases\\" + 
                league + 
                "data" +
                version +
                mod + 
                ".csv");

            TextWriter resWriter;

            /**/
            resWriter = new StreamWriter("..\\..\\..\\..\\Databases\\" + 
                league + 
                "res" + 
                version + 
                mod + 
                ".csv");
            /**/
            resWriter = Console.Out;
            /**/

            var x = new Program();
            x.MakeData(data, writer, resWriter);
            writer.Close();
            resWriter.Close();
        }

        void MakeData(CsvReader res, TextWriter writer, TextWriter resWriter)
        {
            var dataList = res.ToList();

            season = dataList.TakeWhile(x => x[9] == dataList[0][9]).Reverse().ToList();
            previousSeasons = dataList.SkipWhile(x => x[9] == dataList[0][9]).ToList();

            FillSeason();

            var data = new double[inputNeurons];
            var result = new double[outputNeurons];

            var input = new List<double[]>();
            var output = new List<double[]>();

            int i = 0;

            FillTeamsLongTimeStrength();

            ResetSeasonProgress();

            
            var lastSeason = OneMoreSeasonAdd(matches[0].date.Year - 5, out var lastSeasonResults, writer);
            var lastFive = new double[lastSeason.Length * 5][];
            var lastFiveResults = new double[lastSeasonResults.Length * 5][];
            lastSeason.CopyTo(lastFive, 0);
            lastSeasonResults.CopyTo(lastFiveResults, 0);

            ResetSeasonProgress();

            for (int k = 4; k > 0; k--)
            {
                lastSeason = OneMoreSeasonAdd(matches[0].date.Year - k, out lastSeasonResults, writer);

                lastSeason.CopyTo(lastFive, (5 - k) * lastSeason.Length);
                lastSeasonResults.CopyTo(lastFiveResults, (5 - k) * lastSeasonResults.Length);

                ResetSeasonProgress();
            }


            // Count at least 2 rounds, it probably won't help the network
            while (i < teamsInOneSeason)
            {
                AddPoints(matches[i], teams);
                i++;
            }

            int back = i;

            while (i < matches.Count / 2)
            {
                var gamesBefore = TakeGamesBeforeDate(season, matches[i].dateString);

                FillInputData(matches[i], previousSeasons, gamesBefore, teams, out data, out result, writer);

                input.Add(data);

                AddPoints(matches[i], teams);

                output.Add(result);

                i++;
            }


            var inputsArray = new double[input.Count + lastFive.Length][];
            var outputsArray = new double[output.Count + lastFiveResults.Length][];

            input.ToArray().CopyTo(inputsArray, 0);
            lastFive.CopyTo(inputsArray, input.Count);

            output.ToArray().CopyTo(outputsArray, 0);
            lastFiveResults.CopyTo(outputsArray, output.Count);
        

            for(int m = 0; m < inputsArray.Length; m++)
            {
                string str = "";
                int n = 0;

                for(; n < inputsArray[m].Length; n++)
                {
                    str += inputsArray[m][n].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ";";   
                }
                
                for(; n < inputsArray[m].Length + outputsArray[m].Length; n++)
                {
                    int k = n - inputsArray[m].Length;

                    str += outputsArray[m][k];

                    if (n != inputsArray[m].Length + outputsArray[m].Length - 1) str += ";";
                }

                writer.WriteLine(str);
            }

            input = new List<double[]>();
            output = new List<double[]>();

            while (i < matches.Count)
            {
                var gamesBefore = TakeGamesBeforeDate(season, matches[i].dateString);

                FillInputData(matches[i], previousSeasons, gamesBefore, teams, out data, out result, writer);
                

                string str = "";
                for(int n=0; n < data.Length; n++)
                {
                    str += data[n].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ";";
                }

                AddPoints(matches[i], teams);

                double[] resultPlusBet = new double[6];
                for (int j = 0; j < 3; j++)
                {
                    resultPlusBet[j] = result[j];

                    str += resultPlusBet[j] + ";";
                }
                for (int j = 3; j < 6; j++) 
                {
                    resultPlusBet[j] = double.Parse(season[i][j + 7], System.Globalization.CultureInfo.InvariantCulture);

                    str += resultPlusBet[j].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

                    if (j != 5) str += ";";
                }

                resWriter.WriteLine(str);

                i++;
            }

        }

        /// <summary>
        /// Fills list of matches, match by match, field by field
        /// </summary>
        public void FillSeason()
        {

            Match match;

            foreach (var row in season)
            {
                match = FillAMatch(row, table, teams);
                matches.Add(match);
            }
        }

        public void FillSeason(IEnumerable<string[]> stringOfMatches, List<Match> matches, List<Team> table, Dictionary<int, Team> teams)
        {
            foreach (var row in stringOfMatches)
            {
                var match = FillAMatch(row, table, teams);
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

            for (int i = lastSeason; i > firstSeason - 5; i--)
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
            foreach (var pair in teams)
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
        private double[][] OneMoreSeasonAdd(int season, out double[][] seasonResults, TextWriter writer)
        {
            double[][] wholeSeason = new double[360][];
            List<Match> thisSeason = new List<Match>();
            List<Team> seasonsTable = new List<Team>();

            List<double[]> input = new List<double[]>();
            List<double[]> output = new List<double[]>();
            double[] data;
            double[] result;

            var thisYearsTeams = new Dictionary<int, Team>();

            // this season
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
                var gamesBefore = TakeGamesBeforeDate(matches, thisSeason[i].dateString);

                FillInputData(thisSeason[i], seasonsBefore, gamesBefore, teams, out data, out result, writer);

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
        private void FillInputData(Match match, List<string[]> previousSeasons, IEnumerable<string[]> thisSeason, Dictionary<int, Team> teams, out double[] data, out double[] result, TextWriter writer)
        {
            int i = 0;
            data = new double[44];
            result = new double[outputNeurons];

            teams.TryGetValue(match.homeTeamId, out Team homeTeam);
            teams.TryGetValue(match.awayTeamId, out Team awayTeam);

            

            data[i] = homeTeam.wins; if (!inputLogged) writer.Write("htW; "); i++;
            data[i] = homeTeam.draws; if (!inputLogged) writer.Write("htD; "); i++;
            data[i] = homeTeam.loses; if (!inputLogged) writer.Write("htL; "); i++;
            data[i] = (homeTeam.played != 0) ? homeTeam.goalsFor / (double)homeTeam.played : 0; if (!inputLogged) writer.Write("htGFpG; "); i++;
            data[i] = (homeTeam.played != 0) ? homeTeam.goalsAgainst / (double)homeTeam.played : 0; if (!inputLogged) writer.Write("htGApG; "); i++;

            data[i] = awayTeam.wins; if (!inputLogged) writer.Write("atW; "); i++;
            data[i] = awayTeam.draws; if (!inputLogged) writer.Write("atD; "); i++;
            data[i] = awayTeam.loses; if (!inputLogged) writer.Write("atL; "); i++;
            data[i] = (awayTeam.played != 0) ? awayTeam.goalsFor / (double)awayTeam.played : 0; if (!inputLogged) writer.Write("atGFpG; "); i++;
            data[i] = (awayTeam.played != 0) ? awayTeam.goalsAgainst / (double)awayTeam.played : 0; if (!inputLogged) writer.Write("atGApG; "); i++;

            int homeplayed = homeTeam.homewins + homeTeam.homedraws + homeTeam.homeloses;
            data[i] = homeTeam.homewins; if (!inputLogged) writer.Write("htHW; "); i++;
            data[i] = homeTeam.homedraws; if (!inputLogged) writer.Write("htHD; "); i++;
            data[i] = homeTeam.homeloses; if (!inputLogged) writer.Write("htHL; "); i++;
            data[i] = (homeplayed != 0) ? homeTeam.homeGoalsFor / (double)(homeplayed) : 0; if (!inputLogged) writer.Write("htHGFpG; "); i++;
            data[i] = (homeplayed != 0) ? homeTeam.homeGoalsAgainst / (double)(homeplayed) : 0; if (!inputLogged) writer.Write("htHGApG; "); i++;


            int awayplayed = awayTeam.played - (awayTeam.homedraws + awayTeam.homeloses + awayTeam.homewins);
            data[i] = awayTeam.wins - awayTeam.homewins; if (!inputLogged) writer.Write("atAW; "); i++;
            data[i] = awayTeam.draws - awayTeam.homedraws; if (!inputLogged) writer.Write("atAD; "); i++;
            data[i] = awayTeam.loses - awayTeam.homeloses; if (!inputLogged) writer.Write("atAL; "); i++;
            data[i] = (awayplayed != 0) ? (awayTeam.goalsFor - awayTeam.homeGoalsFor) / (double)(awayplayed) : 0;
            if (!inputLogged) writer.Write("atAGFpG; "); i++;
            data[i] = (awayplayed != 0) ? (awayTeam.goalsAgainst - awayTeam.homeGoalsAgainst) / (double)(awayplayed) : 0;
            if (!inputLogged) writer.Write("atAGApG; "); i++;

            GetForm(homeTeam, out var homeFormInput, "h", writer);
            homeFormInput.CopyTo(data, i);
            i += homeFormInput.Length;

            var dfs = homeFormInput[5];
            var dfcs = homeFormInput[6];

            i -= 2;

            GetForm(awayTeam, out var awayFormInput, "a", writer);
            awayFormInput.CopyTo(data, i);
            i += awayFormInput.Length;

            dfs -= awayFormInput[5];
            dfcs -= awayFormInput[6];
            i -= 2;

            GetMutualGames(homeTeam, awayTeam, previousSeasons, thisSeason, out var mutualInput, writer);
            mutualInput.CopyTo(data, i);
            i += mutualInput.Length;

            data[i] = homeTeam.longtimeStrength; if (!inputLogged) writer.Write("htLTS; "); i++;
            data[i] = awayTeam.longtimeStrength; if (!inputLogged) writer.Write("atLTS; "); i++;

            data[i] = dfs; if (!inputLogged) writer.Write("dFS; "); i++;
            data[i] = dfcs; if (!inputLogged) writer.Write("dFCS; "); i++;
            

            if (match.homeTeamGoalsScored > match.awayTeamGoalsScored) result[0] = 1;
            else if (match.homeTeamGoalsScored < match.awayTeamGoalsScored) result[2] = 1;
            else result[1] = 1;

            
            if (!inputLogged) writer.WriteLine("H;D;A");
            inputLogged = true;
        }

        private void GetForm(Team team, out double[] formInput, string logPrefix, TextWriter log)
        {
            var form = new double[7];


            int pointsGot = 0;
            int i = 0;
            while (i < team.currentForm.Length)
            {
                if (team.currentForm[i] == null) break;
                switch (team.currentForm[i].finalpoints)
                {
                    case MatchForm.GameResult.L: form[2]++; break;
                    case MatchForm.GameResult.D: form[1]++; pointsGot = 1; break;
                    case MatchForm.GameResult.W: form[0]++; pointsGot = 3; break;
                }
                form[3] += team.currentForm[i].goalsFor;
                form[4] += team.currentForm[i].goalsAgainst;

                //pseudocoefficient of power, reflects power of the result right after the game ends
                //more recent result therefore has more weight on result
                form[5] += team.currentForm[i].score;

                //pseudocoefficient of power, reflects power of the result now
                //if this team won against a team that was on a hot streak, it had major boost in points
                //therefore this team won against a red hot team, a win that has more theoretical value,
                //there is no weight on recent results
                form[6] += pointsGot * team.currentForm[i].matchAgainst.points;
                i++;

                pointsGot = 0;
            }

            if (i > 0)
            {
                form[3] /= i;
                form[4] /= i;
            }

            formInput = new double[form.Length];
            int j = 0;
            formInput[j] = form[j]; j++; if (!inputLogged) log.Write(logPrefix + "FW; ");
            formInput[j] = form[j]; j++; if (!inputLogged) log.Write(logPrefix + "FD; ");
            formInput[j] = form[j]; j++; if (!inputLogged) log.Write(logPrefix + "FL; ");
            formInput[j] = form[j]; j++; if (!inputLogged) log.Write(logPrefix + "FGF; ");
            formInput[j] = form[j]; j++; if (!inputLogged) log.Write(logPrefix + "FGA; ");
            formInput[j] = form[j]; j++; //if (!inputLogged) log.Write(logPrefix + "FS; ");
            formInput[j] = form[j]; j++; //if (!inputLogged) log.Write(logPrefix + "FCS; ");
        }

        private void GetMutualGames(Team homeTeam, Team awayTeam, List<string[]> previousSeasons, IEnumerable<string[]> thisSeason, out double[] mutualInput, TextWriter log)
        {
            var mutual = FindLastMutualGames(homeTeam.id, awayTeam.id, previousSeasons);

            var thisYearsMutual = FindLastMutualGames(homeTeam.id, awayTeam.id, thisSeason);

            var mutualPreInput = new double[10];
            mutualInput = new double[mutualPreInput.Length];
            
            int j = 0;
            while (thisYearsMutual[j] != null && j < maxMutualMatches)
            {
                mutual[maxMutualMatches - j - 1] = thisYearsMutual[j];
                j++;
            }

            int i = 0;
            int k = 0;
            while (i < mutual.Length)
            {
                if (mutual[i] == null)
                {
                    i++;
                    continue;
                }
                else k++;

                switch (mutual[i].homePoints)
                {
                    case 3: mutualPreInput[0]++; break;
                    case 1: mutualPreInput[1]++; break;
                    case 0: mutualPreInput[2]++; break;
                }

                mutualPreInput[3] += mutual[i].homeGoalsScored;
                mutualPreInput[4] += mutual[i].awayGoalsScored;

                i++;
            }
            if (k != 0)
            {

                mutualPreInput[3] /= k;
                mutualPreInput[4] /= k;
            }

            mutual = FindLastMutualHomeGames(homeTeam.id, awayTeam.id, previousSeasons);

            thisYearsMutual = FindLastMutualHomeGames(homeTeam.id, awayTeam.id, thisSeason);

            j = 0;
            while (thisYearsMutual[j] != null && j < maxMutualMatches)
            {
                mutual[maxMutualMatches - j - 1] = thisYearsMutual[j];
                j++;
            }

            i = 0;
            k = 0;
            while (i < mutual.Length)
            {
                if (mutual[i] == null)
                {
                    i++;
                    continue;
                }
                else k++;

                switch (mutual[i].homePoints)
                {
                    case 3: mutualPreInput[5]++; break;
                    case 1: mutualPreInput[6]++; break;
                    case 0: mutualPreInput[7]++; break;
                }

                mutualPreInput[8] += mutual[i].homeGoalsScored;
                mutualPreInput[9] += mutual[i].awayGoalsScored;

                i++;
            }
            if (k != 0)
            {

                mutualPreInput[8] /= k;
                mutualPreInput[9] /= k;
            }

            j = 0;
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MW; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MD; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("ML; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MGF; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MGA; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MhW; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MhD; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MhL; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MhGF; ");
            mutualInput[j] = mutualPreInput[j]; j++; if (!inputLogged) log.Write("MhGA; ");
        }

        /// <summary>
        /// Finds last games between those two teams regardless of who was playing at home from previous seasons data.
        /// </summary>
        /// <param name="homeTeamId"></param>
        /// <param name="awayTeamId"></param>
        /// <returns></returns>
        public MutualMatch[] FindLastMutualGames(int homeTeamId, int awayTeamId, IEnumerable<string[]> previousSeasons)
        {

            var suitableMatches = previousSeasons.Where(match =>
                (match[5] == homeTeamId.ToString() && match[4] == awayTeamId.ToString()) ||
                (match[5] == awayTeamId.ToString() && match[4] == homeTeamId.ToString()));

            return FillMutualMatches(suitableMatches, homeTeamId, awayTeamId); ;
        }

        /// <summary>
        /// Finds last games between those two teams where home team was playing at home.
        /// </summary>
        /// <param name="homeTeamId"></param>
        /// <param name="awayTeamId"></param>
        /// <returns></returns>
        public MutualMatch[] FindLastMutualHomeGames(int homeTeamId, int awayTeamId, IEnumerable<string[]> previousSeasons)
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

        private IEnumerable<string[]> TakeGamesBeforeDate(IEnumerable<string[]> season, string date)
        {
            return season.Where(match => string.Compare(match[8], date) == -1).Reverse();
        }
    }
}
