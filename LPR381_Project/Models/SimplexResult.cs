using System;
using System.Collections.Generic;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Represents the complete result returned by the Primal Simplex solver.
    ///
    /// This class deliberately contains calculation results rather than
    /// presentation/Console logic. Other algorithms such as Branch & Bound,
    /// Cutting Plane and Sensitivity Analysis can therefore reuse the
    /// Simplex solver without depending on the UI.
    /// </summary>
    public class SimplexResult
    {
        /// <summary>
        /// Possible states of the Simplex solution.
        /// </summary>
        public enum SolverStatus
        {
            Optimal,
            Unbounded,
            Infeasible,
            IterationLimit
        }

        /// <summary>
        /// Final status of the solver.
        /// </summary>
        public SolverStatus Status { get; set; }

        /// <summary>
        /// Human-readable explanation of the final status.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Final objective-function value.
        /// </summary>
        public double ObjectiveValue { get; set; }

        /// <summary>
        /// Values of the variables in the final solution.
        ///
        /// The array corresponds to the columns in the canonical model.
        /// </summary>
        public double[] VariableValues { get; set; }

        /// <summary>
        /// Names of the variables corresponding to VariableValues.
        /// </summary>
        public string[] VariableNames { get; set; }

        /// <summary>
        /// Final simplex tableau.
        /// </summary>
        public double[,] FinalTableau { get; set; }

        /// <summary>
        /// All tableau iterations produced by the solver.
        ///
        /// Iteration 0 is the initial tableau.
        /// </summary>
        public List<SimplexIteration> Iterations { get; set; }

        /// <summary>
        /// Number of pivots performed.
        /// </summary>
        public int PivotCount { get; set; }

        /// <summary>
        /// Index of the final basis row for each basic variable.
        ///
        /// This will become particularly useful for Branch & Bound
        /// and Sensitivity Analysis.
        /// </summary>
        public int[] BasisRows { get; set; }

        public SimplexResult()
        {
            Status = SolverStatus.Infeasible;
            Message = string.Empty;
            ObjectiveValue = 0.0;
            VariableValues = new double[0];
            VariableNames = new string[0];
            FinalTableau = new double[0, 0];
            Iterations = new List<SimplexIteration>();
            BasisRows = new int[0];
            PivotCount = 0;
        }
    }
}