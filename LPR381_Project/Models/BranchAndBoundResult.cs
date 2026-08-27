using System;
using System.Collections.Generic;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Represents the complete result of a Branch & Bound algorithm.
    /// </summary>
    public class BranchAndBoundResult
    {
        /// <summary>
        /// Indicates whether an integer-feasible solution was found.
        /// </summary>
        public bool HasSolution { get; set; }

        /// <summary>
        /// Best integer objective value found.
        /// </summary>
        public double BestObjectiveValue { get; set; }

        /// <summary>
        /// Best original decision-variable values.
        /// </summary>
        public double[] BestVariableValues { get; set; }

        /// <summary>
        /// Names of the original decision variables.
        /// </summary>
        public string[] VariableNames { get; set; }

        /// <summary>
        /// Root of the Branch & Bound search tree.
        /// </summary>
        public BranchAndBoundNode RootNode { get; set; }

        /// <summary>
        /// Flat collection containing every generated node.
        ///
        /// This is useful for output, reporting, and presentation.
        /// </summary>
        public List<BranchAndBoundNode> Nodes { get; set; }

        /// <summary>
        /// Overall Branch & Bound status.
        /// </summary>
        public BranchAndBoundStatus Status { get; set; }

        /// <summary>
        /// Human-readable result message.
        /// </summary>
        public string Message { get; set; }

        public BranchAndBoundResult()
        {
            HasSolution = false;

            BestObjectiveValue = 0.0;

            BestVariableValues =
                Array.Empty<double>();

            VariableNames =
                Array.Empty<string>();

            RootNode = null;

            Nodes =
                new List<BranchAndBoundNode>();

            Status =
                BranchAndBoundStatus.NoIntegerSolution;

            Message =
                string.Empty;
        }
    }

    /// <summary>
    /// Overall status of a Branch & Bound run.
    /// </summary>
    public enum BranchAndBoundStatus
    {
        /// <summary>
        /// An optimal integer solution was found.
        /// </summary>
        Optimal,

        /// <summary>
        /// The problem has no integer-feasible solution.
        /// </summary>
        NoIntegerSolution,

        /// <summary>
        /// The search encountered an unbounded relaxation.
        /// </summary>
        Unbounded,

        /// <summary>
        /// The search reached a configured limit.
        /// </summary>
        IterationLimit
    }
}