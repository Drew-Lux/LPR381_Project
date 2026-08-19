using System;
using System.Collections.Generic;
using System.IO;
using LPR381_Project.Models;

namespace LPR381_Project.Parsers
{
	public class InputParser
	{
		public static ProblemModel ParseFromFile(string filePath)
		{
			var model = new ProblemModel();

			//read all lines from the text file and store in string array
			string[] lines = File.ReadAllLines(filePath);

			if (lines.Length == 0)
			{
				throw new Exception("The input file is empty...");
			}

			//Split line by spaces and remove empty entries
			string[] firstLine = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			model.ObjectiveType = firstLine[0].ToLower(); // max or min

			// The objective function coefficient has the sign and the number
			int numVariables = (firstLine.Length - 1);
			model.ObjectiveCoefficients = new double[numVariables];

			for (int i=0; i < numVariables; i++)
			{
                model.ObjectiveCoefficients[i] = double.Parse(firstLine[1 + i]);
            }

			for (int lineIndex = 1; lineIndex < lines.Length -1; lineIndex++)
			{
				string line = lines[lineIndex].Trim();
				if (string.IsNullOrEmpty(line)) continue; //skip blank lines

				string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				var constraint = new Constraint();
				constraint.Coefficients = new double[numVariables];

				//Every variable has a sign and coefficient value
				for(int i = 0; i < numVariables; i++)
				{
                    constraint.Coefficients[i] = double.Parse(tokens[i]);
                }

				//token after all coefficients is the sign (=,<=,>=)
				constraint.Operator = tokens[numVariables];

				//final token on line is RHS value
				constraint.RightHandSide = double.Parse(tokens[numVariables + 1]);

				model.Constraints.Add(constraint);
			}

			//Parse sign restrictions line
			string lastLine = lines[lines.Length - 1].Trim();
			model.SignRestrictions = lastLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			return model;
		}
	}
}

