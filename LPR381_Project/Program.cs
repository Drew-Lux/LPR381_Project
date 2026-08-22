using System;
using System.Reflection;
using LPR381_Project.Parsers;
using LPR381_Project.Algorithims;
using LPR381_Project.Algorithms;

namespace LPR381_Project
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--Parser test--");
            string filePath = "Inputs/sample_input.txt";

            try
            {
                // 1. Test the Parser
                var rawModel = InputParser.ParseFromFile(filePath);
                Console.WriteLine("\n[SUCCESS] Raw input file parsed successfully.");

                // 2. Test the Canonical Form Converter
                var canonicalModel = CanonicalFormConverter.Convert(rawModel);
                Console.WriteLine("\n[SUCCESS] Converted to Canonical Form!");
                Console.WriteLine($"Objective Type: {canonicalModel.ObjectiveType}");
                Console.WriteLine($"Total Variables (with slack/surplus): {canonicalModel.VaribleNames.Length}");
                Console.WriteLine($"Variable Names: {string.Join(", ", canonicalModel.VaribleNames)}");

                Console.WriteLine("\nObjective Coefficients:");
                Console.WriteLine(string.Join(", ", canonicalModel.ObjectiveCoefficients));

                Console.WriteLine("\nConstraint Matrix Dimensions:");
                int rowCount = canonicalModel.ConstraintMatrix.GetLength(0);
                int colCount = canonicalModel.ConstraintMatrix.GetLength(1);
                Console.WriteLine($"{rowCount} rows x {colCount} columns");

                SimplexSolver.Solve(canonicalModel);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"\nError parsing file: {ex.Message}");
            }

           

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}