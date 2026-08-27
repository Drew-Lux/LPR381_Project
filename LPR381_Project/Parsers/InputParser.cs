using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LPR381_Project.Models;

namespace LPR381_Project.Parsers
{
    /// <summary>
    /// Parses the original LPR381 input-file format.
    /// </summary>
    public static class InputParser
    {
        /// <summary>
        /// Parses an LP/IP model from a text file.
        /// </summary>
        public static ProblemModel ParseFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Input file path cannot be empty.",
                    nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The specified input file could not be found.",
                    filePath);
            }

            string[] rawLines =
                File.ReadAllLines(filePath);

            List<string> lines =
                new List<string>();

            foreach (string rawLine in rawLines)
            {
                string line = rawLine.Trim();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            if (lines.Count < 2)
            {
                throw new FormatException(
                    "The input file must contain an objective, " +
                    "at least one constraint, and sign restrictions.");
            }

            // ---------------------------------------------------------
            // OBJECTIVE
            // ---------------------------------------------------------

            string[] objectiveTokens =
                Tokenize(lines[0]);

            if (objectiveTokens.Length < 2)
            {
                throw new FormatException(
                    "The objective function is missing coefficients.");
            }

            string objectiveType =
                objectiveTokens[0].ToLowerInvariant();

            if (objectiveType != "max" &&
                objectiveType != "min")
            {
                throw new FormatException(
                    "The objective must begin with 'max' or 'min'.");
            }

            int variableCount =
                objectiveTokens.Length - 1;

            double[] objectiveCoefficients =
                new double[variableCount];

            for (int i = 0; i < variableCount; i++)
            {
                objectiveCoefficients[i] =
                    ParseNumber(
                        objectiveTokens[i + 1],
                        $"objective coefficient {i + 1}");
            }

            // ---------------------------------------------------------
            // SIGN RESTRICTIONS
            // ---------------------------------------------------------

            string[] signRestrictions =
                Tokenize(lines[lines.Count - 1]);

            if (signRestrictions.Length != variableCount)
            {
                throw new FormatException(
                    $"Expected {variableCount} sign restrictions, " +
                    $"but found {signRestrictions.Length}.");
            }

            for (int i = 0;
                 i < signRestrictions.Length;
                 i++)
            {
                signRestrictions[i] =
                    signRestrictions[i].ToLowerInvariant();

                if (!IsValidSignRestriction(
                    signRestrictions[i]))
                {
                    throw new FormatException(
                        $"Invalid sign restriction " +
                        $"'{signRestrictions[i]}' for x{i + 1}.");
                }
            }

            // ---------------------------------------------------------
            // CONSTRAINTS
            // ---------------------------------------------------------

            ProblemModel model =
                new ProblemModel();

            model.ObjectiveType =
                objectiveType;

            model.ObjectiveCoefficients =
                objectiveCoefficients;

            model.SignRestrictions =
                signRestrictions;

            for (int lineIndex = 1;
                 lineIndex < lines.Count - 1;
                 lineIndex++)
            {
                Constraint constraint =
                    ParseConstraint(
                        lines[lineIndex],
                        variableCount,
                        lineIndex);

                model.Constraints.Add(
                    constraint);
            }

            if (model.Constraints.Count == 0)
            {
                throw new FormatException(
                    "The model must contain at least one constraint.");
            }

            return model;
        }

        // =============================================================
        // CONSTRAINT PARSING
        // =============================================================

        private static Constraint ParseConstraint(
            string line,
            int variableCount,
            int lineNumber)
        {
            string[] tokens =
                Tokenize(line);

            int expectedTokenCount =
                variableCount + 2;

            if (tokens.Length != expectedTokenCount)
            {
                throw new FormatException(
                    $"Constraint on line {lineNumber + 1} " +
                    $"must contain {expectedTokenCount} tokens.");
            }

            Constraint constraint =
                new Constraint();

            constraint.Coefficients =
                new double[variableCount];

            for (int i = 0;
                 i < variableCount;
                 i++)
            {
                constraint.Coefficients[i] =
                    ParseNumber(
                        tokens[i],
                        $"constraint {lineNumber} coefficient {i + 1}");
            }

            string relation =
                tokens[variableCount];

            if (relation != "=" &&
                relation != "<=" &&
                relation != ">=")
            {
                throw new FormatException(
                    $"Invalid constraint relation " +
                    $"'{relation}' on line {lineNumber + 1}.");
            }

            constraint.Operator =
                relation;

            constraint.RightHandSide =
                ParseNumber(
                    tokens[variableCount + 1],
                    $"constraint {lineNumber} RHS");

            return constraint;
        }

        // =============================================================
        // HELPERS
        // =============================================================

        private static string[] Tokenize(string line)
        {
            return line.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
        }

        private static double ParseNumber(
            string token,
            string description)
        {
            if (!double.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value))
            {
                throw new FormatException(
                    $"Invalid numeric value '{token}' " +
                    $"for {description}.");
            }

            return value;
        }

        private static bool IsValidSignRestriction(
            string restriction)
        {
            return restriction == "+" ||
                   restriction == "-" ||
                   restriction == "urs" ||
                   restriction == "int" ||
                   restriction == "bin";
        }
    }
}