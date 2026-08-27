using System;
using System.Collections.Generic;
using LPR381_Project.Algorithms;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Represents a node in a Branch & Bound search tree.
    ///
    /// A node contains a modified optimization problem together
    /// with the result obtained from solving its LP relaxation.
    /// </summary>
    public class BranchAndBoundNode
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
        /// Depth of the node in the search tree.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Optimization problem represented by this node.
        ///
        /// This is a clone of the original model with any
        /// additional branching constraints applied.
        /// </summary>
        public ProblemModel Model { get; set; }

        /// <summary>
        /// Canonical form of this node's model, kept so the
        /// canonical tableau can be displayed for every sub-problem,
        /// not only the final one.
        /// </summary>
        public CanonicalModel CanonicalForm { get; set; }

        /// <summary>
        /// Simplex result for the LP relaxation at this node.
        /// </summary>
        public SimplexResult RelaxationResult { get; set; }

        /// <summary>
        /// Upper/lower bound represented by the LP relaxation,
        /// depending on the optimization direction.
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
        /// Branching variable index (into the ORIGINAL decision
        /// variables of the root model, not the canonical columns).
        ///
        /// -1 means no branching variable has been selected.
        /// </summary>
        public int BranchingVariableIndex { get; set; } = -1;

        /// <summary>
        /// Value of the branching variable in the LP relaxation.
        /// </summary>
        public double BranchingVariableValue { get; set; }

        /// <summary>
        /// Child nodes generated from this node.
        /// </summary>
        public List<BranchAndBoundNode> Children { get; set; }

        public BranchAndBoundNode()
        {
            Model = null!;
            CanonicalForm = null!;
            RelaxationResult = null!;
            Children = new List<BranchAndBoundNode>();
            FathomReason = FathomReason.None;
        }
    }

    /// <summary>
    /// Describes why a Branch & Bound node was fathomed.
    /// </summary>
    public enum FathomReason
    {
        None,

        /// <summary>
        /// LP relaxation was infeasible.
        /// </summary>
        Infeasible,

        /// <summary>
        /// Node produced an integer-feasible solution.
        /// </summary>
        IntegerSolution,

        /// <summary>
        /// Node bound could not improve the incumbent.
        /// </summary>
        BoundNotBetter,

        /// <summary>
        /// LP relaxation was unbounded.
        /// </summary>
        Unbounded,

        /// <summary>
        /// No valid branching variable could be selected.
        /// </summary>
        NoBranchingVariable
    }
}