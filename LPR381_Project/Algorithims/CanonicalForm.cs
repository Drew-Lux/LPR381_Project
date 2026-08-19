using System;
using System.Collections.Generic;
using LPR381_Project.Models;

namespace LPR381_Project.Algorithims
{
	public class CanonicalModel
	{
		public string ObjectiveType { get; set; } = "max";
		public double[] ObjectiveCoefficients { get; set; } = new double[0];
        public double[,] ConstraintMatrix { get; set; } = new double[0,0];
        public double[] RightHandSide { get; set; } = new double[0];
        public string[] VaribleNames { get; set; } = new string[0];
    }

    public class CanonicalFormConverter
    {
        public static CanonicalModel Convert(ProblemModel rawModel)
        {
            var canonical = new CanonicalModel();
            canonical.ObjectiveType = rawModel.ObjectiveType;

            int originalVarCount = rawModel.ObjectiveCoefficients.Length;
            int constraintCount = rawModel.Constraints.Count;

            //How may extra varibles do we need? (s,a,e varibles?)

            List<string> varNames = new List<string>();
            for (int i =0; i< originalVarCount; i++)
            {
                varNames.Add($"x{i + 1}");
            }
            //Copy of original obj coeff
            List<double> objCoeffs = new List<double>(rawModel.ObjectiveCoefficients);

            double[,] matrix = new double[constraintCount, originalVarCount + constraintCount];
            double[] rhs = new double[constraintCount];

            for(int i = 0; i<constraintCount; i++)
            {
                var constraint = rawModel.Constraints[i];
                rhs[i] = constraint.RightHandSide;

                //copy of original decision var coeff
                for(int j = 0; j<originalVarCount; j++)
                {
                    matrix[i, j] = constraint.Coefficients[j];
                }

                // basic extra varibles based on operator
                if(constraint.Operator == "<=")
                {
                    matrix[i, originalVarCount + i] = 1.0;
                    varNames.Add($"s{i + 1}");
                    objCoeffs.Add(0.0);
                }
                else if (constraint.Operator == ">=")
                {
                    matrix[i, originalVarCount + i] = -1.0;
                    objCoeffs.Add(0.0);
                }
                else if(constraint.Operator == "=")
                {
                    matrix[i, originalVarCount + i] = 1.0; // Artificial / Equality placeholder
                    varNames.Add($"a{i + 1}");
                    objCoeffs.Add(0.0);
                }
            }

            canonical.ObjectiveCoefficients = objCoeffs.ToArray();
            canonical.ConstraintMatrix = matrix;
            canonical.RightHandSide = rhs;
            canonical.VaribleNames = varNames.ToArray();

            return canonical;
        }
    }
}

