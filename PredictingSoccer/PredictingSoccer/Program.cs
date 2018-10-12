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
            double accuracy;
            double averageAcc = 0;
            int tries = 10;

            for (int i = 0; i < tries; i++)
            {
                var data = new CsvReader(new StringReader(Properties.Resources.data), false, ',');
                NeuralNetwork neuralNetwork = new NeuralNetwork(data);
                neuralNetwork.FillSeason();

                Accord.Math.Random.Generator.Seed = i;

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

                accuracy = neuralNetwork.accuracy;
                averageAcc += accuracy;
            }

            averageAcc /= tries;
            Console.WriteLine();
            Console.WriteLine($"Average accuracy: {averageAcc * 100}");
            Console.ReadLine();
        }
    }
}
