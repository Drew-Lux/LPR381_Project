using LPR381_Project.Algorithms;
using LPR381_Project.Models;
using LPR381_Project.Parsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace LPR381_Project
{
    /// <summary>
    /// Menu-driven console front end.
    ///
    /// This class is intentionally "dumb": it holds the current
    /// session state (loaded model, last solve result) and drives
    /// Console I/O only. All calculation logic lives in
    /// LPR381_Project.Algorithms, and all file-export formatting
    /// lives in LPR381_Project.Parsers.OutputExporter.
    /// </summary>
    class Program
    {
        // =============================================================
        // SESSION STATE
        // =============================================================

        private static ProblemModel? _currentModel;
        private static CanonicalModel? _currentCanonical;

        private static SimplexResult? _lastSimplexResult;
        private static RevisedSimplexResult? _lastRevisedSimplexResult;
        private static BranchAndBoundResult? _lastBranchAndBoundResult;
        private static CuttingPlaneResult? _lastCuttingPlaneResult;
        private static KnapsackBranchAndBoundResult? _lastKnapsackResult;

        // Name of whichever algorithm produced the most recent result.
        // Used to decide what ExportResults() should write out.
        private static string _lastAlgorithm = string.Empty;

        private static string _loadedFilePath = string.Empty;

        // =============================================================
        // ENTRY POINT / MAIN MENU LOOP
        // =============================================================

        static void Main(string[] args)
        {
            Console.Clear();
            PrintBanner();

            bool running = true;

            while (running)
            {
                PrintMainMenu();

                string choice =
                    (Console.ReadLine() ?? string.Empty).Trim();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            Console.Clear();
                            LoadInputFile();
                            break;

                        case "2":
                            Console.Clear();
                            DisplayModelInfo();
                            break;

                        case "3":
                            Console.Clear();
                            SolveMenu();
                            break;

                        case "4":
                            Console.Clear();
                            SensitivityMenu();
                            break;

                        case "5":
                            Console.Clear();
                            ExportResults();
                            break;

                        case "0":
                            running = false;
                            break;

                        default:
                            Console.WriteLine();
                            Console.WriteLine(
                                "Invalid selection. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // -------------------------------------------------
                    // Central error handling for every menu action.
                    //
                    // This covers malformed input files, invalid
                    // programming models, and any unexpected solver
                    // failure without crashing the console session.
                    // -------------------------------------------------

                    Console.WriteLine();
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine("ERROR");
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Goodbye.");
        }

        // =============================================================
        // BANNER / MENUS
        // =============================================================

        private static void PrintBanner()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("           LPR381 SOLVER");
            Console.WriteLine("========================================");
        }

        private static void PrintMainMenu()
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("MAIN MENU");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine(
                string.IsNullOrEmpty(_loadedFilePath)
                    ? "Loaded file : (none)"
                    : $"Loaded file : {_loadedFilePath}");

            Console.WriteLine(
                string.IsNullOrEmpty(_lastAlgorithm)
                    ? "Last solve  : (none)"
                    : $"Last solve  : {_lastAlgorithm}");

            Console.WriteLine();
            Console.WriteLine("1) Load input file");
            Console.WriteLine("2) Display model / canonical form");
            Console.WriteLine("3) Solve programming model");
            Console.WriteLine("4) Sensitivity analysis");
            Console.WriteLine("5) Export results to output file");
            Console.WriteLine("0) Exit");
            Console.Write("> ");
        }

        // =============================================================
        // 1. LOAD INPUT FILE
        // =============================================================

        private static void LoadInputFile()
        {
            Console.WriteLine();
            Console.Write(
                "Enter path to input file " +
                "(leave blank for Inputs/sample_input.txt): ");

            string? path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path))
            {
                path = "Inputs/sample_input.txt";
            }

            ProblemModel model =
                InputParser.ParseFromFile(path);

            CanonicalModel canonical =
                CanonicalFormConverter.Convert(model);

            // Reset all session state - a new model invalidates any
            // previous solve result.
            _currentModel = model;
            _currentCanonical = canonical;
            _lastSimplexResult = null;
            _lastBranchAndBoundResult = null;
            _lastCuttingPlaneResult = null;
            _lastKnapsackResult = null;
            _lastAlgorithm = string.Empty;
            _loadedFilePath = path;

            Console.WriteLine();
            Console.WriteLine($"[SUCCESS] Loaded and parsed '{path}'.");

            DisplayModelInfo();
        }

        // =============================================================
        // 2. DISPLAY MODEL / CANONICAL FORM
        // =============================================================

        private static void DisplayModelInfo()
        {
            if (_currentModel == null || _currentCanonical == null)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "No model has been loaded yet. Choose option 1 first.");

                return;
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("MODEL INFORMATION");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine($"Objective: {_currentCanonical.ObjectiveType}");

            Console.WriteLine(
                $"Original variables: " +
                $"{_currentCanonical.OriginalVariableCount}");

            Console.WriteLine(
                $"Original constraints: " +
                $"{_currentCanonical.OriginalConstraintCount}");

            Console.WriteLine(
                $"Canonical variables: " +
                $"{_currentCanonical.VariableNames.Length}");

            Console.WriteLine(
                $"Canonical constraints: " +
                $"{_currentCanonical.ConstraintMatrix.GetLength(0)}");

            Console.WriteLine(
                $"Requires Phase 1: " +
                $"{_currentCanonical.RequiresPhaseOne}");

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("CANONICAL FORM");
            Console.WriteLine("----------------------------------------");

            DisplayCanonicalForm(_currentCanonical);
        }

        // =============================================================
        // 3. SOLVE
        // =============================================================

        private static void SolveMenu()
        {
            if (_currentModel == null)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "No model has been loaded yet. Choose option 1 first.");

                return;
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("SELECT ALGORITHM");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("1) Primal Simplex");
            Console.WriteLine("2) Revised Primal Simplex");
            Console.WriteLine("3) Branch & Bound Simplex");
            Console.WriteLine("4) Cutting Plane");
            Console.WriteLine("5) Branch & Bound Knapsack");
            Console.WriteLine("0) Back");
            Console.Write("> ");

            string choice =
                (Console.ReadLine() ?? string.Empty).Trim();

            switch (choice)
            {
                case "1":
                    Console.Clear();
                    SolvePrimalSimplex();
                    break;

                case "2":
                    Console.Clear();
                    SolveRevisedSimplex();
                    break;

                case "3":
                    Console.Clear();
                    SolveBranchAndBoundSimplex();
                    break;

                case "4":
                    Console.Clear();
                    SolveCuttingPlane();
                    break;

                case "5":
                    Console.Clear();
                    SolveBranchAndBoundKnapsack();
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine();
                    Console.WriteLine("Invalid selection.");
                    break;
            }
        }

        private static void SolvePrimalSimplex()
        {
            // Re-convert in case the model was solved before and the
            // canonical form needs to be freshly built for display.
            CanonicalModel canonical =
                CanonicalFormConverter.Convert(_currentModel!);

            _currentCanonical = canonical;

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("CANONICAL FORM");
            Console.WriteLine("----------------------------------------");

            DisplayCanonicalForm(canonical);

            Console.WriteLine();
            Console.WriteLine("Solving...");

            SimplexResult result =
                SimplexSolver.Solve(canonical);

            _lastSimplexResult = result;
            _lastBranchAndBoundResult = null;
            _lastAlgorithm = "Primal Simplex";

            Console.WriteLine();
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Pivot count: {result.PivotCount}");

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("ITERATIONS");
            Console.WriteLine("----------------------------------------");

            DisplayIterations(
                result.Iterations,
                canonical.VariableNames);

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("FINAL RESULT");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine(
                $"Objective value: {result.ObjectiveValue:F3}");

            Console.WriteLine();
            Console.WriteLine("VARIABLE VALUES");

            DisplayVariableValues(
                result.VariableNames,
                result.VariableValues);
        }

        private static void SolveRevisedSimplex()
        {
            CanonicalModel canonical =
                CanonicalFormConverter.Convert(_currentModel!);

            _currentCanonical = canonical;

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("CANONICAL FORM");
            Console.WriteLine("----------------------------------------");

            DisplayCanonicalForm(canonical);

            Console.WriteLine();
            Console.WriteLine("Solving...");

            RevisedSimplexResult result =
                RevisedSimplexSolver.Solve(canonical);

            _lastRevisedSimplexResult = result;
            _lastSimplexResult = null;
            _lastBranchAndBoundResult = null;
            _lastKnapsackResult = null;
            _lastAlgorithm = "Revised Primal Simplex";

            Console.WriteLine();
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Pivot count: {result.PivotCount}");

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("PRODUCT FORM / PRICE OUT ITERATIONS");
            Console.WriteLine("----------------------------------------");

            DisplayRevisedIterations(result.Iterations);

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("FINAL RESULT");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine($"Objective value: {result.ObjectiveValue:F3}");

            Console.WriteLine();
            Console.WriteLine("VARIABLE VALUES");

            DisplayVariableValues(result.VariableNames, result.VariableValues);
        }

        private static void SolveBranchAndBoundSimplex()
        {
            Console.WriteLine();
            Console.WriteLine("Solving with Branch & Bound Simplex...");

            BranchAndBoundResult result =
                BranchAndBoundSolver.Solve(_currentModel!);

            _lastBranchAndBoundResult = result;
            _lastSimplexResult = null;
            _lastAlgorithm = "Branch & Bound Simplex";

            Console.WriteLine();
            Console.WriteLine(
                $"Total sub-problems (nodes) generated: " +
                $"{result.Nodes.Count}");

            foreach (BranchAndBoundNode node in result.Nodes)
            {
                Console.WriteLine();
                Console.WriteLine("========================================");

                Console.WriteLine(
                    $"NODE {node.NodeId}  " +
                    $"(parent {node.ParentNodeId}, depth {node.Depth})");

                Console.WriteLine("========================================");

                if (node.CanonicalForm == null ||
                    node.RelaxationResult == null)
                {
                    Console.WriteLine(
                        "This node was not processed " +
                        "(node limit reached before it was explored).");

                    continue;
                }

                Console.WriteLine();
                Console.WriteLine("Canonical form:");

                DisplayCanonicalForm(node.CanonicalForm);

                Console.WriteLine();
                Console.WriteLine("Iterations:");

                DisplayIterations(
                    node.RelaxationResult.Iterations,
                    node.CanonicalForm.VariableNames);

                Console.WriteLine();

                Console.WriteLine(
                    $"Relaxation status: {node.RelaxationResult.Status}, " +
                    $"objective: {node.RelaxationResult.ObjectiveValue:F3}");

                if (node.IsFathomed)
                {
                    Console.WriteLine(
                        $"FATHOMED - Reason: {node.FathomReason}");
                }
                else if (node.BranchingVariableIndex >= 0)
                {
                    Console.WriteLine(
                        $"Branching on x{node.BranchingVariableIndex + 1} " +
                        $"= {node.BranchingVariableValue:F3} " +
                        $"-> child nodes " +
                        $"{node.Children[0].NodeId} (<=) and " +
                        $"{node.Children[1].NodeId} (>=)");
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("BEST CANDIDATE");
            Console.WriteLine("----------------------------------------");

            if (result.HasSolution)
            {
                Console.WriteLine(
                    $"Objective value: {result.BestObjectiveValue:F3}");

                Console.WriteLine();
                Console.WriteLine("VARIABLE VALUES");

                DisplayVariableValues(
                    result.VariableNames,
                    result.BestVariableValues);
            }
            else
            {
                Console.WriteLine(result.Message);
            }
        }

        private static void SolveCuttingPlane()
        {
            CanonicalModel canonical =
                CanonicalFormConverter.Convert(_currentModel!);

            _currentCanonical = canonical;

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("CANONICAL FORM (LP RELAXATION)");
            Console.WriteLine("----------------------------------------");

            DisplayCanonicalForm(canonical);

            Console.WriteLine();
            Console.WriteLine("Solving...");

            CuttingPlaneResult result =
                CuttingPlaneSolver.Solve(_currentModel!);

            _lastCuttingPlaneResult = result;
            _lastSimplexResult = null;
            _lastRevisedSimplexResult = null;
            _lastBranchAndBoundResult = null;
            _lastKnapsackResult = null;
            _lastAlgorithm = "Cutting Plane";

            Console.WriteLine();
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Cuts added: {result.Cuts.Count}");
            Console.WriteLine($"Pivot count: {result.PivotCount}");

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("PRODUCT FORM / PRICE OUT ITERATIONS");
            Console.WriteLine("----------------------------------------");

            DisplayRevisedIterations(result.Iterations);

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("GOMORY CUTS GENERATED");
            Console.WriteLine("----------------------------------------");

            if (result.Cuts.Count == 0)
            {
                Console.WriteLine("No cuts were required.");
            }
            else
            {
                foreach (GomoryCut cut in result.Cuts)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Cut {cut.CutNumber}:");
                    Console.WriteLine($"  Source row     : {cut.SourceRow + 1}");
                    Console.WriteLine($"  Source variable: {cut.SourceVariable} = {cut.SourceValue:F3}");
                    Console.WriteLine($"  Slack variable : {cut.SlackVariableName}");
                    Console.WriteLine($"  Inequality     : {cut.Description}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("FINAL RESULT");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine($"Objective value: {result.ObjectiveValue:F3}");

            Console.WriteLine();
            Console.WriteLine("VARIABLE VALUES");

            DisplayVariableValues(result.VariableNames, result.VariableValues);
        }

        private static void SolveBranchAndBoundKnapsack()
        {
            Console.WriteLine();
            Console.WriteLine("Solving with Branch & Bound Knapsack...");

            KnapsackBranchAndBoundResult result =
                BranchAndBoundKnapsackSolver.Solve(_currentModel!);

            _lastKnapsackResult = result;
            _lastSimplexResult = null;
            _lastBranchAndBoundResult = null;
            _lastAlgorithm = "Branch & Bound Knapsack";

            // The solver rejects models that aren't shaped like a 0/1 knapsack
            // (e.g. more than one constraint, non-binary variables, etc.) by
            // returning HasSolution = false with zero nodes generated.
            if (result.Nodes.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine(result.Message);
                return;
            }

            Console.WriteLine();
            Console.WriteLine(
                $"Total sub-problems (nodes) generated: {result.Nodes.Count}");

            Console.WriteLine();
            Console.WriteLine("Items sorted by value/weight ratio (highest first):");

            for (int i = 0; i < result.SortedItemOriginalIndex.Length; i++)
            {
                int originalIndex = result.SortedItemOriginalIndex[i];

                Console.WriteLine(
                    $"  {result.VariableNames[originalIndex]} -> " +
                    $"ratio {result.SortedItemRatio[i]:F3}");
            }

            foreach (KnapsackBranchAndBoundNode node in result.Nodes)
            {
                Console.WriteLine();
                Console.WriteLine("========================================");

                Console.WriteLine(
                    $"NODE {node.NodeId} (parent {node.ParentNodeId}, " +
                    $"level {node.Level})");

                Console.WriteLine("========================================");

                Console.WriteLine(
                    DescribeDecisions(
                        node.Decisions,
                        result.SortedItemOriginalIndex,
                        result.VariableNames));

                Console.WriteLine($"Total weight: {node.TotalWeight:F3}");
                Console.WriteLine($"Total value: {node.TotalValue:F3}");
                Console.WriteLine($"Bound: {node.Bound:F3}");

                if (node.IsFathomed)
                {
                    Console.WriteLine($"FATHOMED - Reason: {node.FathomReason}");
                }
                else if (node.Children.Count == 2 &&
                         node.Level < result.SortedItemOriginalIndex.Length)
                {
                    int nextItemOriginalIndex =
                        result.SortedItemOriginalIndex[node.Level];

                    string nextItemName =
                        result.VariableNames[nextItemOriginalIndex];

                    Console.WriteLine(
                        $"Branching on {nextItemName} -> child nodes " +
                        $"{node.Children[0].NodeId} (include) and " +
                        $"{node.Children[1].NodeId} (exclude)");
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("BEST CANDIDATE");
            Console.WriteLine("----------------------------------------");

            if (result.HasSolution)
            {
                Console.WriteLine($"Objective value: {result.BestObjectiveValue:F3}");

                Console.WriteLine();
                Console.WriteLine("VARIABLE VALUES");

                DisplayVariableValues(
                    result.VariableNames,
                    result.BestVariableValues);
            }
            else
            {
                Console.WriteLine(result.Message);
            }
        }

        // =============================================================
        // 4. SENSITIVITY ANALYSIS (placeholder)
        // =============================================================

        private static void SensitivityMenu()
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("SENSITIVITY ANALYSIS");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine(
                "Not implemented yet. Planned operations:");

            Console.WriteLine(
                " 1) Range of a selected Non-Basic Variable");

            Console.WriteLine(
                " 2) Apply a change to a selected Non-Basic Variable");

            Console.WriteLine(
                " 3) Range of a selected Basic Variable");

            Console.WriteLine(
                " 4) Apply a change to a selected Basic Variable");

            Console.WriteLine(
                " 5) Range of a selected constraint RHS");

            Console.WriteLine(
                " 6) Apply a change to a selected constraint RHS");

            Console.WriteLine(
                " 7) Range of a variable in a Non-Basic column");

            Console.WriteLine(
                " 8) Apply a change to a variable in a Non-Basic column");

            Console.WriteLine(
                " 9) Add a new activity to the optimal solution");

            Console.WriteLine(
                "10) Add a new constraint to the optimal solution");

            Console.WriteLine(
                "11) Display shadow prices");

            Console.WriteLine(
                "12) Apply duality / solve the dual / verify strong-weak duality");
        }

        // =============================================================
        // 5. EXPORT RESULTS
        // =============================================================

        private static void ExportResults()
        {
            if (string.IsNullOrEmpty(_lastAlgorithm))
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Nothing has been solved yet. Choose option 3 first.");

                return;
            }

            Console.WriteLine();
            Console.Write(
                "Enter output file path " +
                "(leave blank for Outputs/output.txt): ");

            string? path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path))
            {
                path = "Outputs/output.txt";
            }

            string? directory =
                Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (_lastAlgorithm == "Primal Simplex" &&
                _lastSimplexResult != null &&
                _currentCanonical != null)
            {
                OutputExporter.ExportPrimalSimplex(
                    path,
                    _currentModel!,
                    _currentCanonical,
                    _lastSimplexResult);
            }
            else if (_lastAlgorithm == "Branch & Bound Simplex" &&
                     _lastBranchAndBoundResult != null)
            {
                OutputExporter.ExportBranchAndBound(
                    path,
                    _currentModel!,
                    _lastBranchAndBoundResult);
            }
            else if (_lastAlgorithm == "Branch & Bound Knapsack" &&
                    _lastKnapsackResult != null)
            {
                OutputExporter.ExportBranchAndBoundKnapsack(
                    path,
                    _currentModel!,
                    _lastKnapsackResult);
            }
            else if (_lastAlgorithm == "Revised Primal Simplex" &&
                     _lastRevisedSimplexResult != null &&
                     _currentCanonical != null)
            {
                RevisedSimplexExporter.Export(
                    path,
                    _currentCanonical,
                    _lastRevisedSimplexResult);
            }
            else if (_lastAlgorithm == "Cutting Plane" &&
                     _lastCuttingPlaneResult != null)
            {
                CuttingPlaneExporter.Export(
                    path,
                    _currentCanonical!,
                    _lastCuttingPlaneResult);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine(
                    "No exportable result is available for " +
                    $"'{_lastAlgorithm}'.");

                return;
            }

            Console.WriteLine();
            Console.WriteLine($"[SUCCESS] Results exported to '{path}'.");
        }

        // =============================================================
        // DISPLAY HELPERS
        // =============================================================

        /// <summary>
        /// Displays a canonical model as a table: constraint rows
        /// followed by the objective row, exactly as the model reads
        /// before any simplex pivoting takes place.
        /// </summary>
        private static void DisplayCanonicalForm(
            CanonicalModel canonical)
        {
            int constraintCount =
                canonical.ConstraintMatrix.GetLength(0);

            int variableCount =
                canonical.ConstraintMatrix.GetLength(1);

            double[,] display =
                new double[constraintCount + 1, variableCount + 1];

            for (int i = 0; i < constraintCount; i++)
            {
                for (int j = 0; j < variableCount; j++)
                {
                    display[i, j] =
                        canonical.ConstraintMatrix[i, j];
                }

                display[i, variableCount] =
                    canonical.RightHandSide[i];
            }

            for (int j = 0; j < variableCount; j++)
            {
                display[constraintCount, j] =
                    canonical.ObjectiveCoefficients[j];
            }

            DisplayTableau(
                display,
                canonical.VariableNames,
                "Obj");
        }

        private static void DisplayIterations(
            List<SimplexIteration> iterations,
            string[] variableNames)
        {
            if (iterations == null || iterations.Count == 0)
            {
                Console.WriteLine("No iterations were recorded.");
                return;
            }

            foreach (SimplexIteration iteration in iterations)
            {
                Console.WriteLine();

                Console.WriteLine(
                    $"Iteration: {iteration.IterationNumber}" +
                    (iteration.IsFinal ? "  (final tableau)" : string.Empty));

                if (!string.IsNullOrWhiteSpace(iteration.EnteringVariable))
                {
                    Console.WriteLine(
                        $"Entering: {iteration.EnteringVariable}");
                }

                if (!string.IsNullOrWhiteSpace(iteration.LeavingVariable))
                {
                    Console.WriteLine(
                        $"Leaving: {iteration.LeavingVariable}");
                }

                if (iteration.PivotRow >= 0)
                {
                    Console.WriteLine(
                        $"Pivot row: {iteration.PivotRow}");
                }

                if (iteration.PivotColumn >= 0)
                {
                    Console.WriteLine(
                        $"Pivot column: {iteration.PivotColumn}");
                }

                DisplayTableau(
                    iteration.Tableau,
                    variableNames,
                    "Z");
            }
        }

        private static void DisplayRevisedIterations(
    List<RevisedSimplexIteration> iterations)
        {
            if (iterations == null || iterations.Count == 0)
            {
                Console.WriteLine("No iterations were recorded.");
                return;
            }

            foreach (RevisedSimplexIteration iteration in iterations)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"Iteration {iteration.IterationNumber} - {iteration.Stage}" +
                    (iteration.IsFinal ? "  (final)" : string.Empty));

                Console.WriteLine($"Basis: {string.Join(", ", iteration.BasisNames)}");

                DisplayVector("C_B", iteration.BasicCosts);
                DisplayMatrix("B", iteration.BasisMatrix);
                DisplayMatrix("B^-1", iteration.BasisInverse);
                DisplayVector("Prices (C_B * B^-1)", iteration.Prices);
                DisplayVector("Reduced costs (C_j - y*A_j)", iteration.ReducedCosts);

                if (iteration.EnteringColumn >= 0)
                {
                    Console.WriteLine($"Entering: {iteration.EnteringVariable}");
                    DisplayVector("Direction (B^-1 * A_j)", iteration.Direction);
                    DisplayVector("Ratio test", iteration.Ratios);

                    if (iteration.LeavingRow >= 0)
                    {
                        Console.WriteLine(
                            $"Leaving: {iteration.LeavingVariable} " +
                            $"(row {iteration.LeavingRow + 1})");
                    }

                    Console.WriteLine($"Pivot value: {iteration.PivotValue:F3}");
                    Console.WriteLine("Product form - Eta matrix:");
                    DisplayMatrix("Eta", iteration.EtaMatrix);
                }

                Console.WriteLine($"Objective value: {iteration.ObjectiveValue:F3}");
            }
        }

        private static void DisplayVector(string label, double[] values)
        {
            if (values == null || values.Length == 0) return;

            Console.Write($"{label}: ");
            foreach (double value in values) Console.Write($"{value:F3} ");
            Console.WriteLine();
        }

        private static void DisplayMatrix(string label, double[,] matrix)
        {
            Console.WriteLine($"{label}:");

            if (matrix == null || matrix.Length == 0)
            {
                Console.WriteLine("(none)");
                return;
            }

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                    Console.Write($"{matrix[i, j],10:F3}");
                Console.WriteLine();
            }
        }
        private static void DisplayVariableValues(
            string[] names,
            double[] values)
        {
            if (values == null)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string name =
                    names != null && i < names.Length
                        ? names[i]
                        : $"x{i + 1}";

                Console.WriteLine($"{name} = {values[i]:F3}");
            }
        }

        private static string DescribeDecisions(
            int[] decisions,
            int[] sortedItemOriginalIndex,
        string[] variableNames)
        {
            List<string> parts = new List<string>();

            for (int i = 0; i < decisions.Length; i++)
            {
                string name =
                    variableNames[sortedItemOriginalIndex[i]];

                string state =
                    decisions[i] switch
                    {
                        1 => "in",
                        0 => "out",
                        _ => "?"
                    };

                parts.Add($"{name}={state}");
            }

            return "Decisions: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Prints a matrix as a labelled table. The final row is
        /// labelled using <paramref name="lastRowLabel"/> ("Obj" for
        /// an unsolved canonical form, "Z" for a simplex tableau).
        /// </summary>
        private static void DisplayTableau(
            double[,] tableau,
            string[] variableNames,
            string lastRowLabel)
        {
            if (tableau == null)
            {
                Console.WriteLine("No tableau available.");
                return;
            }

            int rows = tableau.GetLength(0);
            int columns = tableau.GetLength(1);

            Console.WriteLine();

            Console.Write($"{"Row",-8}");

            for (int j = 0; j < columns - 1; j++)
            {
                string name =
                    variableNames != null && j < variableNames.Length
                        ? variableNames[j]
                        : $"V{j + 1}";

                Console.Write($"{name,10}");
            }

            Console.Write($"{"RHS",10}");
            Console.WriteLine();

            for (int i = 0; i < rows; i++)
            {
                string rowName =
                    i == rows - 1
                        ? lastRowLabel
                        : $"C{i + 1}";

                Console.Write($"{rowName,-8}");

                for (int j = 0; j < columns; j++)
                {
                    Console.Write($"{tableau[i, j],10:F3}");
                }

                Console.WriteLine();
            }
        }
    }
}