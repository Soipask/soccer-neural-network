using Accord.Controls;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Kernels;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;
using Accord.MachineLearning.Bayes;
using Accord.IO;

using System;
using System.Data;
using Accord.Neuro;
using Accord.Neuro.Learning;

namespace ConsoleApp27
{
    class Program
    {
        public void DoXOR()
        {
            //Program for XOR
            double[][] inputs = {
                new double[] {0,1},
                new double[] {0,0},
                new double[] {1,1},
                new double[] {1,0}
            };

            int[] outputs = { 1, 0, 0, 1 };

            var smo = new SequentialMinimalOptimization<Gaussian>();
            smo.Complexity = 100;

            var svm = smo.Learn(inputs, outputs);
            bool[] predictions = svm.Decide(inputs);

            double error = new AccuracyLoss(outputs).Loss(predictions);

            Console.WriteLine("Error: " + error);

            ScatterplotBox.Show("Training data", inputs, outputs);
            ScatterplotBox.Show("SVM results", inputs, predictions.ToZeroOne());

            Console.ReadLine();

        }
        static void Main(string[] args)
        {
            DataTable table = new ExcelReader("examples.xls").GetWorksheet("Classification - Yin Yang");
            double[][] inputs = table.ToJagged<double>("x", "Y");
            int[] outputs = table.Columns["G"].ToArray<int>();

            // Since we would like to learn binary outputs in the form
            // [-1,+1], we can use a bipolar sigmoid activation function
            IActivationFunction function = new BipolarSigmoidFunction();

            // In our problem, we have 2 inputs (x, y pairs), and we will 
            // be creating a network with 5 hidden neurons and 1 output:
            //
            var network = new ActivationNetwork(function,
                inputsCount: 2, neuronsCount: new[] { 5, 1 });

            // Create a Levenberg-Marquardt algorithm
            var teacher = new ResilientBackpropagationLearning(network)
            {
               // UseRegularization = true
            };
            teacher.LearningRate = 0.1;

            // Because the network is expecting multiple outputs,
            // we have to convert our single variable into arrays
            //
            var y = outputs.ToDouble().ToArray();

            var f = outputs.ToDouble().ToJagged();
            // Iterate until stop criteria is met
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
                // Compute one learning iteration
                error = teacher.RunEpoch(inputs, f);

            } while (error >= 0.1);

            Console.Write("Epoch: " + epoch + "\t");
            Console.WriteLine("Error: " + error);


            // Classify the samples using the model
            int[] answers = inputs.Apply(network.Compute).GetColumn(0).Apply(System.Math.Sign);

            // Plot the results
            ScatterplotBox.Show("Expected results", inputs, outputs);
            ScatterplotBox.Show("Network results", inputs, answers)
            .Hold();
        }
    }
}
