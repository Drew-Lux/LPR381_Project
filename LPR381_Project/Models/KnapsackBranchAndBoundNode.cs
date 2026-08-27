using System;
using System.Collections.Generic;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Represents one sub-problem (node) in the Branch & Bound
    /// Knapsack search tree.
    ///
    /// Unlike <see cref="BranchAndBoundNode"/>, a knapsack node does
    /// not carry a simplex tableau. Its bound is computed directly
    /// from the fractional-relaxation (Dantzig) upper bound, which
    /// is far cheaper than solving an LP at every node and is the
    /// standard technique for 0/1 knapsack Branch & Bound.
    /// </summary>
    public class KnapsackBranchAndBoundNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Identifier of the parent node.
        /// Root node has ParentNodeId = -1.
        /// </summary>
        public int ParentNodeId { get; set; } = -1;

        /// <summary>
        /// Number of items already decided (index into the
        /// efficiency-sorted item list of the NEXT item to branch
        /// on). Level == item count means every item has a decision,
        /// i.e. this node is a leaf.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Decision for every item, in efficiency-sorted order:
        ///
        ///  1  = item included
        ///  0  = item excluded
        /// -1  = not yet decided
        /// </summary>
        public int[] Decisions { get; set; }

        /// <summary>
        /// Total weight consumed by the items included so far.
        /// </summary>
        public double TotalWeight { get; set; }

        /// <summary>
        /// Total value accumulated by the items included so far.
        /// </summary>
        public double TotalValue { get; set; }

        /// <summary>
        /// Fractional-relaxation upper bound on the best value
        /// achievable from this node onward.
        /// </summary>
        public double Bound { get; set; }

        /// <summary>
        /// Indicates whether this node has been fathomed.
        /// </summary>
        public bool IsFathomed { get; set; }

        /// <summary>
        /// Explanation for why the node was fathomed.
        /// </summary>
        public FathomReason FathomReason { get; set; }

        /// <summary>
        /// Child nodes generated from this node
        /// (Children[0] = include branch, Children[1] = exclude branch).
        /// </summary>
        public List<KnapsackBranchAndBoundNode> Children { get; set; }

        public KnapsackBranchAndBoundNode()
        {
            Decisions = Array.Empty<int>();
            Children = new List<KnapsackBranchAndBoundNode>();
            FathomReason = FathomReason.None;
        }
    }
}