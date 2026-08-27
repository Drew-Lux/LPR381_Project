using System;
using System.Collections.Generic;
using LPR381_Project.Algorithms;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Complete result returned by the Cutting Plane solver.
    ///
    /// Mirrors the shape of <see cref="RevisedSimplexResult"/> so the
    /// existing Program.cs display helpers (DisplayRevisedIterations,
    /// DisplayVariableValues, etc.) can be reused without
    /// modification, and adds the sequence of Gomory cuts that were
    /// generated along the way.
    /// </summary>
    public class CuttingPlaneResult
    {
        public SimplexResult.SolverStatus Status { get; set; }
            = SimplexResult.SolverStatus.Infeasible;

        public string Message { get; set; } = string.Empty;

        public double ObjectiveValue { get; set; }

        public string[] VariableNames { get; set; } = Array.Empty<string>();

        public double[] VariableValues { get; set; } = Array.Empty<double>();

        public int[] BasisColumns { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Total number of simplex/dual-simplex pivots performed,
        /// across the initial relaxation and every cut.
        /// </summary>
        public int PivotCount { get; set; }

        /// <summary>
        /// Every Gomory cut generated, in the order they were added.
        /// Empty when the LP relaxation was already integer-feasible.
        /// </summary>
        public List<GomoryCut> Cuts { get; set; } = new List<GomoryCut>();

        /// <summary>
        /// Every Product Form / Price Out iteration produced, covering
        /// the initial Revised Primal Simplex relaxation (Stage
        /// "Phase 1" / "Phase 2") and every subsequent Dual Simplex
        /// re-optimisation after a cut was added (Stage
        /// "Cut {n} - Dual Simplex").
        /// </summary>
        public List<RevisedSimplexIteration> Iterations { get; set; }
            = new List<RevisedSimplexIteration>();

        /// <summary>
        /// The final canonical model, including every Gomory cut
        /// constraint and slack column that was appended. Used by the
        /// exporter/console display to show the final tableau shape.
        /// </summary>
        public CanonicalModel? FinalCanonical { get; set; }
    }
}
