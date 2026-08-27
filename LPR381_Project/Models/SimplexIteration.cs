using System;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Stores all relevant information about one iteration
    /// of the Primal Simplex algorithm.
    ///
    /// The tableau is copied at the time the iteration is recorded
    /// so that later changes to the working tableau do not modify
    /// previously stored iterations.
    /// </summary>
    public class SimplexIteration
    {
        /// <summary>
        /// Iteration number.
        /// Iteration 0 represents the initial tableau.
        /// </summary>
        public int IterationNumber { get; set; }

        /// <summary>
        /// Tableau at this iteration.
        /// The final column is the RHS column.
        /// </summary>
        public double[,] Tableau { get; set; }

        /// <summary>
        /// Pivot row selected during this iteration.
        /// -1 means that no pivot row was selected.
        /// </summary>
        public int PivotRow { get; set; }

        /// <summary>
        /// Pivot column selected during this iteration.
        /// -1 means that no pivot column was selected.
        /// </summary>
        public int PivotColumn { get; set; }

        /// <summary>
        /// Name of the entering variable.
        /// </summary>
        public string EnteringVariable { get; set; }

        /// <summary>
        /// Name of the leaving variable.
        /// </summary>
        public string LeavingVariable { get; set; }

        /// <summary>
        /// Value of the pivot element.
        /// </summary>
        public double PivotValue { get; set; }

        /// <summary>
        /// Indicates whether this iteration represents
        /// the final tableau.
        /// </summary>
        public bool IsFinal { get; set; }

        public SimplexIteration()
        {
            Tableau = new double[0, 0];
            PivotRow = -1;
            PivotColumn = -1;
            EnteringVariable = string.Empty;
            LeavingVariable = string.Empty;
        }
    }
}