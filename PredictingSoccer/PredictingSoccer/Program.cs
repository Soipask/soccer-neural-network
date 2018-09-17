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
    class Program
    {
        static void Main(string[] args)
        {
            var season = new CsvReader(new StringReader(Properties.Resources.england11), false, ',');
            var prevSeasons = new CsvReader(new StringReader(Properties.Resources.englandupto11), false, ',');
            NeuralNetwork neuralNetwork = new NeuralNetwork(season, prevSeasons);
            neuralNetwork.FillSeason();
            

            /*
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
            }*/

            neuralNetwork.MakeScoreNeuralNetwork();
        }
    }
}
