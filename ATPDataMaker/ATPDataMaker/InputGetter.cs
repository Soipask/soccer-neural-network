using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ATPDataMaker
{
    class InputGetter
    {
        private readonly TextReader data;
        private readonly TextReader ranksData;
        private readonly TextWriter writer;
        private readonly TextWriter resWriter;
        private readonly TextWriter testWriter;

        private List<string[]> season;
        private List<string[]> prevSeasons;

        private Dictionary<int, Player> allPlayers;
        //key is id
        private List<string[]> playerRankTable;
        //directly from cvs: 0-id, 1..n-ranks
        private int firstRankedYear;

        private int seasonsEvaluated;
        private int maxForm;

        private int seed = 0;
        private bool headerWritten = false;

        public InputGetter(TextReader data, TextReader ranksData, TextWriter writer, TextWriter resWriter, TextWriter testWriter, int seasonsToTrain = 5, int maxForm = 10)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            this.data = data;
            this.ranksData = ranksData;
            this.writer = writer;
            this.resWriter = resWriter;
            this.testWriter = testWriter;
            seasonsEvaluated = seasonsToTrain;
            this.maxForm = maxForm;

            allPlayers = new Dictionary<int, Player>();
            playerRankTable = new List<string[]>();
        }

        public void MakeData()
        {
            List<string[]> allGames = new List<string[]>();

            string line;
            
            
            // reading file
            while ((line = data.ReadLine()) != null)
            {
                allGames.Add(line.Split(';'));
            }

            // getting rankings info
            string header = ranksData.ReadLine();
            string[] headerSplit = header.Split(',');
            firstRankedYear = int.Parse(headerSplit[2]);
            int stringLength = headerSplit.Length - 1;
            
            while ((line = ranksData.ReadLine()) != null)
            {
                string[] playerRank = line.Split(',');
                Player player = new Player(playerRank[1], int.Parse(playerRank[0]));
                for (int i = 1; i < stringLength; i++)
                {
                    playerRank[i] = playerRank[i + 1];
                }
                playerRankTable.Add(playerRank);
                allPlayers.Add(player.id, player);
            }

            // starting 
            int lastYear = int.Parse(allGames[allGames.Count - 1][2]);
            prevSeasons = allGames.TakeWhile(x => int.Parse(x[2]) != lastYear - seasonsEvaluated).ToList();
            List<double[]> input = new List<double[]>();

            PreparePlayers(lastYear - seasonsEvaluated - 2);
            for (int i = lastYear - seasonsEvaluated - 1; i < lastYear - 2; i++)
            {
                season = allGames.SkipWhile(x => int.Parse(x[2]) != i).TakeWhile(x => int.Parse(x[2]) == i).ToList();
                var seasonInTour = ReverseTournaments(season);

                foreach (var m in seasonInTour)
                {
                    Match match = new Match(m);

                    bool valuable = true;

                    Player winner;
                    if (match.winnerId >= 0) { allPlayers.TryGetValue(match.winnerId, out winner); }
                    else { winner = new Player("not", -1) { rank = 130 }; valuable = false; }

                    Player loser;
                    if (match.loserId >= 0) allPlayers.TryGetValue(match.loserId, out loser);
                    else { loser = new Player("not", -1) { rank = 130 }; valuable = false; }

                    if (winner.rank > 100 || loser.rank > 100) valuable = false;

                    if (!match.havePoints || match.pointsForTournament < 500) valuable = false;

                    if (valuable) { input.Add(FillInputData(winner, loser, match)); }

                    UpdatePlayersProfile(winner, loser, match);
                    prevSeasons.Add(m);
                }
                ResetSeason(i);
            }

            var shuffledInput = ShuffleGames(input,seed);
            foreach (var datapoint in shuffledInput)
            {
                writer.Write(datapoint[0]);
                for (int i = 1; i < datapoint.Length; i++)
                {
                    writer.Write(String.Format("; {0:0.####}", datapoint[i]));
                }
                writer.Write("\n");
            }

            // take one season out for better training
            List<double[]> trainInput = new List<double[]>();
            season = allGames.SkipWhile(x => int.Parse(x[2]) != lastYear - 1).TakeWhile(x => int.Parse(x[2]) == lastYear - 1).ToList();
            var seasonReversed = ReverseTournaments(season);

            foreach (var m in seasonReversed)
            {
                Match match = new Match(m);

                bool valuable = true;

                Player winner;
                if (match.winnerId >= 0) { allPlayers.TryGetValue(match.winnerId, out winner); }
                else { winner = new Player("not", -1); valuable = false; }

                Player loser;
                if (match.loserId >= 0) allPlayers.TryGetValue(match.loserId, out loser);
                else { loser = new Player("not", -1); valuable = false; }

                if (winner.rank > 100 || loser.rank > 100) valuable = false;

                if (!match.havePoints || match.pointsForTournament < 500) valuable = false;

                if (valuable) {
                    var theInput = FillInputData(winner, loser, match);

                    // add bets
                    double[] inputBets = new double[theInput.Length + 2];
                    theInput.CopyTo(inputBets, 0);
                    inputBets[inputBets.Length - 2] = double.Parse(m[m.Length - 2]);
                    inputBets[inputBets.Length - 1] = double.Parse(m[m.Length - 1]);

                    if (inputBets[inputBets.Length - 2] < 1) Console.WriteLine("{0} {6} {7} \t {1} {5} {8} \t {2} {3} {4}", winner.name, loser.name, match.tournament, match.year, match.pointsForTournament, loser.rank, winner.rank, winner.id, loser.id);
                    trainInput.Add(inputBets);
                };

                UpdatePlayersProfile(winner, loser, match);
                prevSeasons.Add(m);
            }

            ResetSeason(lastYear - 1);

            var shuffledTestInput = ShuffleGames(trainInput,seed);
            foreach (var datapoint in shuffledTestInput)
            {
                resWriter.Write(datapoint[0]);
                for (int i = 1; i < datapoint.Length; i++)
                {
                    resWriter.Write(String.Format("; {0:0.####}", datapoint[i]));
                }
                resWriter.Write("\n");
            }

            // last season untouched until getting final results
            List<double[]> testInput = new List<double[]>();
            season = allGames.SkipWhile(x => int.Parse(x[2]) != lastYear).TakeWhile(x => int.Parse(x[2]) == lastYear).ToList();
            seasonReversed = ReverseTournaments(season);

            foreach (var m in seasonReversed)
            {
                Match match = new Match(m);

                bool valuable = true;

                Player winner;
                if (match.winnerId >= 0) { allPlayers.TryGetValue(match.winnerId, out winner); }
                else { winner = new Player("not", -1); valuable = false; }

                Player loser;
                if (match.loserId >= 0) allPlayers.TryGetValue(match.loserId, out loser);
                else { loser = new Player("not", -1); valuable = false; }

                if (winner.rank > 100 || loser.rank > 100) valuable = false;

                if (!match.havePoints || match.pointsForTournament < 500) valuable = false;

                if (valuable)
                {
                    var theInput = FillInputData(winner, loser, match);

                    // add bets
                    double[] inputBets = new double[theInput.Length + 2];
                    theInput.CopyTo(inputBets, 0);
                    inputBets[inputBets.Length - 2] = double.Parse(m[m.Length - 2]);
                    inputBets[inputBets.Length - 1] = double.Parse(m[m.Length - 1]);

                    if (inputBets[inputBets.Length - 2] < 1) Console.WriteLine("{0} {6} {7} \t {1} {5} {8} \t {2} {3} {4}", winner.name, loser.name, match.tournament, match.year, match.pointsForTournament, loser.rank, winner.rank, winner.id, loser.id);

                    testInput.Add(inputBets);
                };

                UpdatePlayersProfile(winner, loser, match);
                prevSeasons.Add(m);
            }
            

            shuffledTestInput = ShuffleGames(testInput, seed);
            foreach (var datapoint in shuffledTestInput)
            {
                testWriter.Write(datapoint[0]);
                for (int i = 1; i < datapoint.Length; i++)
                {
                    testWriter.Write(String.Format("; {0:0.####}", datapoint[i]));
                }
                testWriter.Write("\n");
            }

        }

        private double[] FillInputData(Player winner, Player loser, Match match)
        {
            var input = new List<double>();

            // winner data 
            if (!headerWritten) writer.Write("1W; 1L; 1GDpS");
            input.Add(winner.wins);
            input.Add(winner.loses);
            input.Add(winner.GameDiffPerSet);

            // loser data
            if (!headerWritten) writer.Write("; 2W; 2L; 2GDpS");
            input.Add(loser.wins);
            input.Add(loser.loses);
            input.Add(loser.GameDiffPerSet);

            // winner form data
            if (!headerWritten) writer.Write("; 1FW; 1FL; 1FGDpS");
            var winForm = new double[4];
            for (int i = 0; i < maxForm; i++)
            {
                if (winner.currentForm[i] == null) break;
                // winForm -> [wins, played, gamediff, score]
                winForm[0] += winner.currentForm[i].point;
                winForm[1]++;
                winForm[2] = winner.currentForm[i].gameDiffPerSet;
                winForm[3] += winner.currentForm[i].score;
            }
            winForm[2] = (winForm[1] > 0) ? winForm[2] / winForm[1] : 0;

            input.Add(winForm[0]);
            input.Add(winForm[1] - winForm[0]);
            input.Add(winForm[2]);

            // loser form data
            if (!headerWritten) writer.Write("; 2FW; 2FL; 2FGDpS");
            var losForm = new double[4];
            for (int i = 0; i < maxForm; i++)
            {
                if (loser.currentForm[i] == null) break;
                // losForm -> [wins, played, gamediff, score]
                losForm[0] += loser.currentForm[i].point;
                losForm[1]++;
                losForm[2] = loser.currentForm[i].gameDiffPerSet;
                losForm[3] += loser.currentForm[i].score;
            }
            losForm[2] = (losForm[1] > 0) ? losForm[2] / losForm[1] : 0;

            input.Add(losForm[0]);
            input.Add(losForm[1] - losForm[0]);
            input.Add(losForm[2]);

            
            double[] surfaceInput = new double[3];
            SurfaceInfo wSurface;
            SurfaceInfo lSurface;
            switch (match.surface)
            {
                case "Hard":
                    surfaceInput[0] = 1;
                    wSurface = winner.hard;
                    lSurface = loser.hard;
                    break;
                case "Clay":
                    surfaceInput[1] = 1;
                    wSurface = winner.clay;
                    lSurface = loser.clay;
                    break;
                case "Grass":
                    surfaceInput[2] = 1;
                    wSurface = winner.grass;
                    lSurface = loser.grass;
                    break;
                default: wSurface = lSurface = null; break;
            }
            // winner surface data
            if (!headerWritten) writer.Write("; 1SW; 1SL; 1SGDpS");
            input.Add(wSurface.wins);
            input.Add(wSurface.loses);
            input.Add(wSurface.GameDiffPerSet);

            // loser surface data
            if (!headerWritten) writer.Write("; 2SW; 2SL; 2SGDpS");
            input.Add(lSurface.wins);
            input.Add(lSurface.loses);
            input.Add(lSurface.GameDiffPerSet);

            // winner surface form data
            if (!headerWritten) writer.Write("; 1SFW; 1SFL; 1SFGDpS");
            var ws = new double[4];
            for (int i = 0; i < maxForm; i++)
            {
                if (wSurface.form[i] == null) break;
                ws[0] += wSurface.form[i].point;
                ws[1]++;
                ws[2] = wSurface.form[i].gameDiffPerSet;
                ws[3] += wSurface.form[i].score;
            }
            ws[2] = (ws[1] > 0) ? ws[2] / ws[1] : 0;

            input.Add(ws[0]);
            input.Add(ws[1] - ws[0]);
            input.Add(ws[2]);

            // loser surface form data
            if (!headerWritten) writer.Write("; 2SFW; 2SFL; 2SFGDpS");
            var ls = new double[4];
            for (int i = 0; i < maxForm; i++)
            {
                if (lSurface.form[i] == null) break;
                ls[0] += lSurface.form[i].point;
                ls[1]++;
                ls[2] = lSurface.form[i].gameDiffPerSet;
                ls[3] += lSurface.form[i].score;
            }
            ls[2] = (ls[1] > 0) ? ls[2] / ls[1] : 0;

            input.Add(ls[0]);
            input.Add(ls[1] - ls[0]);
            input.Add(ls[2]);

            // mutual games data
            if (!headerWritten) writer.Write("; 1MW; 1ML; 1MGDpS");
            var ourMutual = GetMutualMatches(winner, loser);
            var mm = new double[3];
            for (int i = 0; i < ourMutual.Count; i++)
            {
                mm[0] += ourMutual[i].player1Won;
                mm[1]++;
                mm[2] += ourMutual[i].gameDiffPerSet;
            }
            mm[2] = (mm[1] > 0) ? mm[2] / mm[1] : 0;
            input.Add(mm[0]);
            input.Add(mm[1] - mm[0]);
            input.Add(mm[2]);

            // mutual surface data
            if (!headerWritten) writer.Write("; 1MSW; 1MSL; 1MSGDpS");
            var ourSurfaceMutual = GetMutualSurfaceMatches(winner, loser, match.surface);
            var msm = new double[3];
            for (int i = 0; i < ourSurfaceMutual.Count; i++)
            {
                msm[0] += ourSurfaceMutual[i].player1Won;
                msm[1]++;
                msm[2] += ourSurfaceMutual[i].gameDiffPerSet;
            }
            msm[2] = (msm[1] > 0) ? msm[2] / msm[1] : 0;
            input.Add(msm[0]);
            input.Add(msm[1] - msm[0]);
            input.Add(msm[2]);

            // winner rank, loser rank, surface, scoreDifference
            if (!headerWritten) writer.Write("; 1R; 2R; H; C; G; dSc; dSSc");
            input.Add(winner.rank);
            input.Add(loser.rank);
            input.Add(surfaceInput[0]);
            input.Add(surfaceInput[1]);
            input.Add(surfaceInput[2]);
            input.Add(winForm[3] - losForm[3]);
            input.Add(ws[3] - ls[3]);

            // result
            if (!headerWritten) { writer.WriteLine("; 1; 2"); headerWritten = true; };
            input.Add(1);
            input.Add(0);

            var ret = input.ToArray();
            return ret;
        }

        private void PreparePlayers(int season)
        {
            foreach(Player p in allPlayers.Values)
            {
                if (!int.TryParse(playerRankTable[p.id][season - firstRankedYear + 1], out p.rank)) p.rank = 130;
                p.EndSeason();
            }
        }

        private void ResetSeason(int year)
        {
            PreparePlayers(year);

        }
        private void UpdatePlayersProfile(Player winner, Player loser, Match match)
        {
            UpdateBasicPlayerInfo(winner, loser, match);
            UpdatePlayersForm(winner, loser, match);
            UpdateSurfaceInfo(winner, loser, match);
            
        }
        private void UpdateBasicPlayerInfo(Player winner, Player loser, Match match)
        {
            winner.wins++; loser.loses++;
            winner.played++; loser.played++;
            winner.gameDiffSum += match.gameDiffPerSet;
            loser.gameDiffSum -= match.gameDiffPerSet;
        }

        private void UpdatePlayersForm(Player winner, Player loser, Match match)
        {
            if (winner.id != -1)
            {
                MatchFormGame win = new MatchFormGame(winner, loser, match);
                for (int i = winner.currentForm.Length - 1; i > 0; i--)
                {
                    winner.currentForm[i] = winner.currentForm[i - 1];
                }

                winner.currentForm[0] = win;
            }
            if (loser.id != -1)
            {
                MatchFormGame lose = new MatchFormGame(loser, winner, match);
                for (int i = loser.currentForm.Length - 1; i > 0; i--)
                {
                    loser.currentForm[i] = loser.currentForm[i - 1];
                }

                loser.currentForm[0] = lose;
            }
        }

        private void UpdateSurfaceInfo(Player winner, Player loser, Match match)
        {
            switch (match.surface)
            {
                case "Hard":
                    if (winner.id != -1) winner.hard.UpdateSurface(winner, loser, match);
                    if (loser.id != -1) loser.hard.UpdateSurface(loser, winner, match);
                    break;
                case "Clay":
                    if (winner.id != -1) winner.clay.UpdateSurface(winner, loser, match);
                    if (loser.id != -1) loser.clay.UpdateSurface(loser, winner, match);
                    break;
                case "Grass":
                    if (winner.id != -1) winner.grass.UpdateSurface(winner, loser, match);
                    if (loser.id != -1) loser.grass.UpdateSurface(loser, winner, match);
                    break;
            }
        }
        
        private List<MutualMatch> GetMutualMatches(Player winner, Player loser)
        {
            var suitable = prevSeasons.Where(match =>
                (match[8] == winner.id.ToString() && match[9] == loser.id.ToString()) ||
                (match[9] == winner.id.ToString() && match[8] == loser.id.ToString()));
            
            var ret = new List<MutualMatch>();

            foreach(var match in suitable)
            {
                MutualMatch mm = (winner.id.ToString() == match[8]) ? 
                    new MutualMatch(match[8], match[9], match[12], match[13]) : 
                    new MutualMatch(match[9], match[8], match[13], match[12]);

                ret.Add(mm);
            }

            return ret;
        }

        private List<MutualMatch> GetMutualSurfaceMatches(Player winner, Player loser, string surface)
        {
            var suitable = prevSeasons.Where(match => match[3] == surface &
                   ((match[8] == winner.id.ToString() && match[9] == loser.id.ToString()) ||
                   (match[9] == winner.id.ToString() && match[8] == loser.id.ToString())));

            var ret = new List<MutualMatch>();

            foreach (var match in suitable)
            {
                MutualMatch msm = (winner.id.ToString() == match[8]) ?
                    new MutualMatch(match[8], match[9], match[12], match[13]) :
                    new MutualMatch(match[9], match[8], match[13], match[12]);

                ret.Add(msm);
            }

            return ret;
        }

        private List<string[]> ReverseTournaments(List<string[]> season)
        {
            var tournamentNames = season.Select(x=>x[0]).Distinct();
            List<List<string[]>> seasonInTour = new List<List<string[]>>();


            foreach(var name in tournamentNames)
            {
                var tournament = season.SkipWhile(x => x[0] != name).TakeWhile(x => x[0] == name).Reverse().ToList();
                seasonInTour.Add(tournament);
            }

            var seasonBack = seasonInTour.SelectMany(x => x).ToList();

            return seasonBack;
        }
        
        /// <summary>
        /// Picks some games, where it swaps winner's and loser's data
        /// </summary>
        /// <param name="input"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private List<double[]> ShuffleGames(List<double[]> input, int seed)
        {
            Random rnd = new Random(seed);
            List<double[]> shuffled = new List<double[]>();

            foreach (var game in input)
            {
                if (rnd.NextDouble() > 0.5)
                {
                    shuffled.Add(ShuffleOne(game));
                }
                else shuffled.Add(game);
            }
            return shuffled;
        }
        /// <summary>
        /// Swaps winner's data with loser's.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private double[] ShuffleOne(double[] match)
        {
            // (Kind of) Hardcoded sequence:
            var shuffled = new double[match.Length];

            var n = 0;
            for (int i = 0; i < 4; i++) //-,F,S,FS
            {
                for (int j = 0; j < 3; j++) // W,L,GDpS
                {
                    shuffled[6 * i + j] = match[6 * i + j + 3]; //2 <-> 1
                    shuffled[6 * i + j + 3] = match[6 * i + j]; //1 <-> 2
                }
            }

            n = 24;
            for (int i = 0; i < 2; i++) //MM, MSM
            {
                shuffled[3 * i + n] = match[3 * i + n + 1];  // W -> L
                shuffled[3 * i + n + 1] = match[3 * i + n];  // L -> W
                shuffled[3 * i + n + 2] = -match[3 * i + n + 2];  //GDpS -> -GDpS
            }
            n = 30;

            shuffled[n] = match[n + 1]; //1R
            shuffled[n + 1] = match[n]; //2R

            n = 32;

            for (; n < 35; n++) //surface stays
                shuffled[n] = match[n];

            n = 35;

            shuffled[n] = -match[n]; //-dFSc
            shuffled[n + 1] = -match[n + 1]; //-dFSSc

            n = 37;

            n = 39;
            shuffled[n - 1] = match[n - 2]; //1
            shuffled[n - 2] = match[n - 1]; //2

            if (match.Length > 39)
            { 
                // and bets
            shuffled[n] = match[n + 1];
            shuffled[n + 1] = match[n];
            }
            return shuffled;
        }
    }
}
