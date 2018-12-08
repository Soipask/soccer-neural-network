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
            string s = DateTime.Now.ToString("yyyyMMddHHmmss");
            string logName = s + "fra" + "ger" + "eng" + ".txt";
            var log = new StreamWriter(logName);
            double averageAcc = 0, betting = 0, realBetting = 0;
            double[] accuracy = new double[3];
            double[] bet = new double[3];
            double[] realBet = new double[3];
            int tries = 20;
            bool headLogged = false;
            for (int i = 0; i < tries; i++)
            {
                var data = new CsvReader(new StringReader(Properties.Resources.fra), false, ',');
                if (!headLogged) log.WriteLine("FRENCH LEAGUE:\n");
                NeuralNetwork neuralNetwork = new NeuralNetwork(data, log, headLogged);
                neuralNetwork.FillSeason();

                Accord.Math.Random.Generator.Seed = i;

                neuralNetwork.MakeScoreNeuralNetwork();
                
                accuracy[0] += neuralNetwork.accuracy;

                bet[0] += neuralNetwork.sumBetting;
                realBet[0] += neuralNetwork.sumRealBetting;
                
                log.WriteLine($"{i + 1}. try:");
                log.WriteLine($"Accuracy: {neuralNetwork.accuracy}");
                log.WriteLine($"Profit: {neuralNetwork.sumBetting}");
                log.WriteLine($"Close games only profit: {neuralNetwork.sumRealBetting}");
                log.WriteLine();

                headLogged = true;
            }

            accuracy[0] /= tries;
            bet[0] /= tries;
            realBet[0] /= tries;

            log.WriteLine();
            log.WriteLine($"Average league stats:");
            log.WriteLine($"Accuracy: {accuracy[0]}");
            log.WriteLine($"Profit: {bet[0]}");
            log.WriteLine($"Close games only profit: {realBet[0]}");
            log.WriteLine("--------------------------------------------------");

            headLogged = false;
            for (int i = 0; i < tries; i++)
            {
                var data = new CsvReader(new StringReader(Properties.Resources.ger), false, ',');
                if (!headLogged) log.WriteLine("GERMAN LEAGUE:\n");
                NeuralNetwork neuralNetwork = new NeuralNetwork(data, log, headLogged);
                neuralNetwork.FillSeason();

                Accord.Math.Random.Generator.Seed = i;

                neuralNetwork.MakeScoreNeuralNetwork();

                accuracy[1] += neuralNetwork.accuracy;

                bet[1] += neuralNetwork.sumBetting;
                realBet[1] += neuralNetwork.sumRealBetting;

                log.WriteLine($"{i + 1}. try:");
                log.WriteLine($"Accuracy: {neuralNetwork.accuracy}");
                log.WriteLine($"Profit: {neuralNetwork.sumBetting}");
                log.WriteLine($"Close games only profit: {neuralNetwork.sumRealBetting}");
                log.WriteLine();

                headLogged = true;
            }

            accuracy[1] /= tries;
            bet[1] /= tries;
            realBet[1] /= tries;

            log.WriteLine();
            log.WriteLine($"Average league stats:");
            log.WriteLine($"Accuracy: {accuracy[1]}");
            log.WriteLine($"Profit: {bet[1]}");
            log.WriteLine($"Close games only profit: {realBet[1]}");
            log.WriteLine("--------------------------------------------------");


            headLogged = false;
            for (int i = 0; i < tries; i++)
            {
                var data = new CsvReader(new StringReader(Properties.Resources.eng), false, ',');
                if (!headLogged) log.WriteLine("ENGLISH LEAGUE:\n");
                NeuralNetwork neuralNetwork = new NeuralNetwork(data, log, headLogged);
                neuralNetwork.FillSeason();

                Accord.Math.Random.Generator.Seed = i;

                neuralNetwork.MakeScoreNeuralNetwork();

                accuracy[2] += neuralNetwork.accuracy;

                bet[2] += neuralNetwork.sumBetting;
                realBet[2] += neuralNetwork.sumRealBetting;

                log.WriteLine($"{i + 1}. try:");
                log.WriteLine($"Accuracy: {neuralNetwork.accuracy}");
                log.WriteLine($"Profit: {neuralNetwork.sumBetting}");
                log.WriteLine($"Close games only profit: {neuralNetwork.sumRealBetting}");
                log.WriteLine();

                headLogged = true;
            }

            accuracy[2] /= tries;
            bet[2] /= tries;
            realBet[2] /= tries;

            log.WriteLine();
            log.WriteLine($"Average league stats:");
            log.WriteLine($"Accuracy: {accuracy[2]}");
            log.WriteLine($"Profit: {bet[2]}");
            log.WriteLine($"Close games only profit: {realBet[2]}");
            log.WriteLine("--------------------------------------------------");

            for (int i = 0; i < accuracy.Length; i++)
            {
                averageAcc += accuracy[i];
                betting += bet[i];
                realBetting += realBet[i];
            }
            averageAcc /= accuracy.Length;
            betting /= accuracy.Length;
            realBetting /= accuracy.Length;

            Console.WriteLine();
            Console.WriteLine($"Average accuracy: {averageAcc * 100}%");
            Console.WriteLine($"Average profit: {betting * 100} Kč.");
            Console.WriteLine($"Average profit in close games: {realBetting * 100} Kč");

            log.WriteLine();
            log.WriteLine("Imagine we bet 100 Kč on each game...");
            log.WriteLine($"Average accuracy: {averageAcc * 100}%");
            log.WriteLine($"Average profit: {betting * 100} Kč.");
            log.WriteLine($"Average profit in close games: {realBetting * 100} Kč");
            log.Close();
        }
    }
}
