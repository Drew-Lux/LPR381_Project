using System;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Stores the calculation information produced during one
    /// iteration of the Revised Primal Simplex method.
    ///
    /// The class deliberately stores both Product Form (eta matrix)
    /// and Price Out information so the assignment can display the
    /// actual revised-simplex calculations rather than only a final
    /// answer.
    /// </summary>
    public class RevisedSimplexIteration
    {
        public int IterationNumber { get; set; }
        public int[] BasisColumns { get; set; }
        public string[] BasisNames { get; set; }
        public double[,] BasisMatrix { get; set; }
        public double[,] BasisInverse { get; set; }
        public double[] BasicCosts { get; set; }
        public double[] Prices { get; set; }
        public double[] ReducedCosts { get; set; }
        public double[] Direction { get; set; }
        public double[] Ratios { get; set; }
        public int EnteringColumn { get; set; }
        public int LeavingRow { get; set; }
        public string EnteringVariable { get; set; }
        public string LeavingVariable { get; set; }
        public double PivotValue { get; set; }
        public double ObjectiveValue { get; set; }
        public double[,] EtaMatrix { get; set; }
        public bool IsFinal { get; set; }
        public string Stage { get; set; }

        public RevisedSimplexIteration()
        {
            BasisColumns = Array.Empty<int>();
            BasisNames = Array.Empty<string>();
            BasisMatrix = new double[0, 0];
            BasisInverse = new double[0, 0];
            BasicCosts = Array.Empty<double>();
            Prices = Array.Empty<double>();
            ReducedCosts = Array.Empty<double>();
            Direction = Array.Empty<double>();
            Ratios = Array.Empty<double>();
            EnteringColumn = -1;
            LeavingRow = -1;
            EnteringVariable = string.Empty;
            LeavingVariable = string.Empty;
            EtaMatrix = new double[0, 0];
            Stage = "Phase 2";
        }
    }
}
