using System;
using System.Collections.Generic;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Stores the complete result of the Branch & Bound Knapsack
    /// algorithm.
    ///
    /// This class contains calculation information only.
    /// Presentation and file-export logic remain separate.
    /// </summary>
    public class KnapsackBranchAndBoundResult
    {
        /// <summary>
        /// Indicates whether a valid solution was found.
        ///
        /// This will be false either because the model was
        /// infeasible, or because the supplied model was not
        /// shaped like a 0/1 knapsack problem (see Message).
        /// </summary>
        public bool HasSolution { get; set; }

        /// <summary>
        /// Best objective value found.
        /// </summary>
        public double BestObjectiveValue { get; set; }

        /// <summary>
        /// Best 0/1 solution found, indexed by ORIGINAL variable
        /// order (not the efficiency-sorted order used internally).
        /// </summary>
        public double[] BestVariableValues { get; set; }

        /// <summary>
        /// Names of the variables corresponding to BestVariableValues.
        /// </summary>
        public string[] VariableNames { get; set; }

        /// <summary>
        /// Root node of the search tree.
        /// </summary>
        public KnapsackBranchAndBoundNode? RootNode { get; set; }

        /// <summary>
        /// All nodes generated during the search.
        /// </summary>
        public List<KnapsackBranchAndBoundNode> Nodes { get; set; }

        /// <summary>
        /// Human-readable status message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Maps each efficiency-sorted position to the ORIGINAL
        /// variable index. SortedItemOriginalIndex[0] is the item
        /// with the highest value/weight ratio.
        /// </summary>
        public int[] SortedItemOriginalIndex { get; set; }

        /// <summary>
        /// Value/weight ratio for each efficiency-sorted position,
        /// parallel to SortedItemOriginalIndex.
        /// </summary>
        public double[] SortedItemRatio { get; set; }

        public KnapsackBranchAndBoundResult()
        {
            HasSolution = false;
            BestObjectiveValue = 0.0;

            BestVariableValues = Array.Empty<double>();
            VariableNames = Array.Empty<string>();

            Nodes = new List<KnapsackBranchAndBoundNode>();

            Message = string.Empty;

            SortedItemOriginalIndex = Array.Empty<int>();
            SortedItemRatio = Array.Empty<double>();
        }
    }
}