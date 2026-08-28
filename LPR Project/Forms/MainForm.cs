using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LPR381Solver.Errors;
using LPR381Solver.IO;
using LPR381Solver.Models;
using LPR381Solver.Solvers;

namespace LPR381Solver.Forms
{
    /// <summary>
    /// Single-window front end for all six algorithms (Primal Simplex, Revised Primal Simplex,
    /// Sensitivity Analysis, Duality, Branch &amp; Bound, Cutting Plane). One model is loaded on
    /// the "Model" tab and shared by every other tab, exactly like the model that used to be
    /// held in Program.cs's console menu loop.
    /// </summary>
    public class MainForm : Form
    {
        private const string DefaultSampleModel =
            "max +3 +5\n" +
            "+1 +0 <=4\n" +
            "+0 +2 <=12\n" +
            "+3 +2 <=18\n" +
            "+ +\n";

        private LPModel? _model;
        private StandardForm? _sf;
        private SimplexResult? _primalResult;
        private RevisedSimplexResult? _revisedResult;

        // Model tab
        private TextBox _txtModelSource = null!;
        private Label _lblSummary = null!;

        // Simple output tabs
        private RichTextBox _rtbPrimal = null!;
        private RichTextBox _rtbRevised = null!;
        private RichTextBox _rtbCanonical = null!;
        private RichTextBox _rtbBranchBound = null!;
        private RichTextBox _rtbCuttingPlane = null!;

        // Sensitivity & Duality tab
        private RichTextBox _rtbSensitivity = null!;
        private ComboBox _cmbVarColumn = null!;
        private TextBox _txtNewCoeffValue = null!;
        private ComboBox _cmbConstraintRow = null!;
        private TextBox _txtNewRhsValue = null!;
        private TextBox _txtNewActivityObj = null!;
        private TextBox _txtNewActivityCoeffs = null!;
        private TextBox _txtNewConstraintCoeffs = null!;
        private ComboBox _cmbRelation = null!;
        private TextBox _txtNewConstraintRhs = null!;

        public MainForm()
        {
            Text = "LPR381 - LP/IP Solver";
            Width = 1150;
            Height = 780;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildModelTab());
            tabs.TabPages.Add(BuildSimpleTab("Primal Simplex", "Solve with Primal Simplex", BtnPrimal_Click, out _rtbPrimal));
            tabs.TabPages.Add(BuildSimpleTab("Revised Simplex", "Solve with Revised Primal Simplex", BtnRevised_Click, out _rtbRevised));
            tabs.TabPages.Add(BuildSimpleTab("Canonical Form", "Show Canonical Form", BtnCanonical_Click, out _rtbCanonical));
            tabs.TabPages.Add(BuildSensitivityTab());
            tabs.TabPages.Add(BuildSimpleTab("Branch && Bound", "Solve (Branch & Bound)", BtnBranchAndBound_Click, out _rtbBranchBound));
            tabs.TabPages.Add(BuildSimpleTab("Cutting Plane", "Solve (Cutting Plane)", BtnCuttingPlane_Click, out _rtbCuttingPlane));

            Controls.Add(tabs);
        }

        // ==================================================================
        //  Model tab
        // ==================================================================

        private TabPage BuildModelTab()
        {
            var page = new TabPage("Model");

            _txtModelSource = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsTab = true,
                Font = new Font("Consolas", 10f),
                Text = DefaultSampleModel
            };

            _lblSummary = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8),
                Text = "Edit the model text above (or open a file), then click \"Parse / Load Model\"."
            };

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(8)
            };
            var btnOpen = new Button { Text = "Open File...", AutoSize = true };
            btnOpen.Click += BtnOpenFile_Click;
            var btnParse = new Button { Text = "Parse / Load Model", AutoSize = true };
            btnParse.Click += (s, e) => ParseModel();
            top.Controls.Add(btnOpen);
            top.Controls.Add(btnParse);

            // Fill-docked control first, then edge-docked controls.
            page.Controls.Add(_txtModelSource);
            page.Controls.Add(_lblSummary);
            page.Controls.Add(top);

            return page;
        }

        private void BtnOpenFile_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _txtModelSource.Text = File.ReadAllText(dlg.FileName);
                    ParseModel();
                }
                catch (Exception ex)
                {
                    ShowError(new FileException($"Could not open '{dlg.FileName}': {ex.Message}"));
                }
            }
        }

        private void ParseModel()
        {
            try
            {
                _model = InputParser.ParseText(_txtModelSource.Text);
                _sf = StandardForm.Build(_model);
                _primalResult = null;
                _revisedResult = null;

                string note = _model.HasIntegerOrBinaryVars
                    ? "  Primal/Revised/Sensitivity treat int/bin variables as their LP relaxation; " +
                      "use Branch & Bound or Cutting Plane to solve them exactly."
                    : "";
                _lblSummary.Text = $"Loaded: {_model.NumVars} variable(s), {_model.NumConstraints} constraint(s), " +
                                    $"{(_model.IsMax ? "maximise" : "minimise")}.{note}";
                _lblSummary.ForeColor = Color.DarkGreen;

                RefreshSensitivityDropdowns();
            }
            catch (Exception ex)
            {
                var solverEx = ex as SolverException ?? new InputValidationException(ex.Message);
                ErrorHandler.Handle(solverEx);
                _lblSummary.Text = "Failed to load model: " + ErrorHandler.GetMessage(solverEx);
                _lblSummary.ForeColor = Color.Firebrick;
                _model = null;
                _sf = null;
            }
        }

        // ==================================================================
        //  Shared helpers
        // ==================================================================

        private bool RequireModel()
        {
            if (_model == null || _sf == null)
            {
                MessageBox.Show(this, "Load a model first (Model tab).", "No model loaded",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private void ShowError(Exception ex)
        {
            var solverEx = ex as SolverException ?? new InputValidationException(ex.Message);
            ErrorHandler.Handle(solverEx);
            MessageBox.Show(this, ErrorHandler.GetMessage(solverEx), "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SaveTextToFile(string text)
        {
            using var dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", FileName = "output.txt" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try { File.WriteAllText(dlg.FileName, text); }
                catch (Exception ex) { ShowError(new FileException($"Could not save '{dlg.FileName}': {ex.Message}")); }
            }
        }

        /// <summary>Builds a plain "one button, one read-only output box" tab.</summary>
        private TabPage BuildSimpleTab(string tabTitle, string buttonText, EventHandler onClick, out RichTextBox output)
        {
            var page = new TabPage(tabTitle);

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 9.5f)
            };

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(8)
            };
            var btn = new Button { Text = buttonText, AutoSize = true };
            btn.Click += onClick;
            var btnSave = new Button { Text = "Save Output...", AutoSize = true };
            btnSave.Click += (s, e) => SaveTextToFile(rtb.Text);
            top.Controls.Add(btn);
            top.Controls.Add(btnSave);

            page.Controls.Add(rtb);
            page.Controls.Add(top);

            output = rtb;
            return page;
        }

        private static double[] ParseDoubleList(string text, int expectedCount, string what)
        {
            var tokens = (text ?? "").Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != expectedCount)
                throw new InputValidationException($"Expected {expectedCount} value(s) for {what}, got {tokens.Length}.");

            var values = new double[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                var tok = tokens[i].Trim();
                if (tok.StartsWith("+")) tok = tok.Substring(1);
                if (!double.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                    throw new InputValidationException($"Could not parse '{tokens[i]}' as a number for {what}.");
            }
            return values;
        }

        // ==================================================================
        //  Primal / Revised / Canonical form
        // ==================================================================

        private void BtnPrimal_Click(object? sender, EventArgs e)
        {
            if (!RequireModel()) return;
            try
            {
                _primalResult = PrimalSimplex.Solve(_sf!);
                _rtbPrimal.Text = OutputWriter.FormatPrimalResult(_primalResult, _sf!, _model!);
                RefreshSensitivityDropdowns();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnRevised_Click(object? sender, EventArgs e)
        {
            if (!RequireModel()) return;
            try
            {
                _revisedResult = RevisedPrimalSimplex.Solve(_sf!);
                _rtbRevised.Text = OutputWriter.FormatRevisedResult(_revisedResult, _sf!, _model!);
                if (_revisedResult.Status == SolveStatus.Optimal)
                    _primalResult = PrimalSimplex.Solve(_sf!);
                RefreshSensitivityDropdowns();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnCanonical_Click(object? sender, EventArgs e)
        {
            if (!RequireModel()) return;
            _rtbCanonical.Text = OutputWriter.FormatCanonicalForm(_sf!);
        }

        // ==================================================================
        //  Branch & Bound / Cutting Plane
        // ==================================================================

        private void BtnBranchAndBound_Click(object? sender, EventArgs e)
        {
            if (!RequireModel()) return;
            try
            {
                var bb = BranchAndBound.Solve(_model!);
                var sb = new StringBuilder();
                foreach (var line in bb.Log) sb.AppendLine(line);
                sb.AppendLine();
                sb.AppendLine("=== Branch & Bound Result ===");
                sb.AppendLine($"Nodes explored: {bb.NodesExplored}");
                if (bb.Found)
                {
                    sb.AppendLine($"Optimal integer Z = {bb.ObjectiveValue:F3}");
                    for (int j = 0; j < _model!.NumVars; j++)
                        sb.AppendLine($"  x{j + 1} = {bb.Solution[j]:F3}");
                }
                else
                {
                    sb.AppendLine("No integer-feasible solution was found.");
                }
                _rtbBranchBound.Text = sb.ToString();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnCuttingPlane_Click(object? sender, EventArgs e)
        {
            if (!RequireModel()) return;
            try
            {
                var cp = CuttingPlane.Solve(_model!);
                var sb = new StringBuilder();
                foreach (var line in cp.Log) sb.AppendLine(line);
                sb.AppendLine();
                sb.AppendLine("=== Cutting Plane Result ===");
                sb.AppendLine($"Status: {cp.Status}   Cuts added: {cp.CutsAdded}");
                if (cp.Status == SolveStatus.Optimal)
                {
                    sb.AppendLine($"Optimal integer Z = {cp.ObjectiveValue:F3}");
                    for (int j = 0; j < _model!.NumVars; j++)
                        sb.AppendLine($"  x{j + 1} = {cp.OriginalSolution[j]:F3}");
                }
                else if (!string.IsNullOrEmpty(cp.Message))
                {
                    sb.AppendLine(cp.Message);
                }
                _rtbCuttingPlane.Text = sb.ToString();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        // ==================================================================
        //  Sensitivity Analysis & Duality
        // ==================================================================

        private TabPage BuildSensitivityTab()
        {
            var page = new TabPage("Sensitivity && Duality");

            _rtbSensitivity = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f)
            };

            var controlsPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 440,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            var stack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 400
            };

            var btnSolve = new Button { Text = "1) Solve (Primal Simplex)", AutoSize = true };
            btnSolve.Click += BtnSensitivitySolve_Click;
            var btnClear = new Button { Text = "Clear Log", AutoSize = true };
            btnClear.Click += (s, e) => _rtbSensitivity.Clear();
            stack.Controls.Add(Row(btnSolve, btnClear));

            stack.Controls.Add(SectionHeader("Shadow Prices"));
            var btnShadow = new Button { Text = "Show Shadow Prices", AutoSize = true };
            btnShadow.Click += BtnShadowPrices_Click;
            stack.Controls.Add(Row(btnShadow));

            stack.Controls.Add(SectionHeader("Variable's Objective Coefficient"));
            _cmbVarColumn = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnVarRange = new Button { Text = "Show Range", AutoSize = true };
            btnVarRange.Click += BtnVarRange_Click;
            stack.Controls.Add(Row(new Label { Text = "Column:", AutoSize = true }, _cmbVarColumn, btnVarRange));

            _txtNewCoeffValue = new TextBox { Width = 100 };
            var btnApplyCoeff = new Button { Text = "Check New Value", AutoSize = true };
            btnApplyCoeff.Click += BtnApplyCoeff_Click;
            stack.Controls.Add(Row(new Label { Text = "New coefficient:", AutoSize = true }, _txtNewCoeffValue, btnApplyCoeff));

            stack.Controls.Add(SectionHeader("Constraint RHS"));
            _cmbConstraintRow = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnRhsRange = new Button { Text = "Show Range", AutoSize = true };
            btnRhsRange.Click += BtnRhsRange_Click;
            stack.Controls.Add(Row(new Label { Text = "Constraint:", AutoSize = true }, _cmbConstraintRow, btnRhsRange));

            _txtNewRhsValue = new TextBox { Width = 100 };
            var btnApplyRhs = new Button { Text = "Apply RHS Change", AutoSize = true };
            btnApplyRhs.Click += BtnApplyRhs_Click;
            stack.Controls.Add(Row(new Label { Text = "New RHS:", AutoSize = true }, _txtNewRhsValue, btnApplyRhs));

            stack.Controls.Add(SectionHeader("Add New Activity"));
            _txtNewActivityObj = new TextBox { Width = 100 };
            stack.Controls.Add(Row(new Label { Text = "Objective coefficient:", AutoSize = true }, _txtNewActivityObj));
            stack.Controls.Add(Row(new Label { Text = "Constraint coeffs (space-separated, one per row):", AutoSize = true }));
            _txtNewActivityCoeffs = new TextBox { Width = 300 };
            var btnNewActivity = new Button { Text = "Evaluate", AutoSize = true };
            btnNewActivity.Click += BtnNewActivity_Click;
            stack.Controls.Add(Row(_txtNewActivityCoeffs, btnNewActivity));

            stack.Controls.Add(SectionHeader("Add New Constraint"));
            stack.Controls.Add(Row(new Label { Text = "Coeffs (space-separated, one per variable):", AutoSize = true }));
            _txtNewConstraintCoeffs = new TextBox { Width = 300 };
            stack.Controls.Add(Row(_txtNewConstraintCoeffs));
            _cmbRelation = new ComboBox { Width = 60, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbRelation.Items.AddRange(new object[] { "<=", ">=", "=" });
            _cmbRelation.SelectedIndex = 0;
            _txtNewConstraintRhs = new TextBox { Width = 100 };
            var btnNewConstraint = new Button { Text = "Evaluate", AutoSize = true };
            btnNewConstraint.Click += BtnNewConstraint_Click;
            stack.Controls.Add(Row(_cmbRelation, new Label { Text = "RHS:", AutoSize = true }, _txtNewConstraintRhs, btnNewConstraint));

            stack.Controls.Add(SectionHeader("Duality"));
            var btnDuality = new Button { Text = "Build Dual, Solve, Check Duality", AutoSize = true };
            btnDuality.Click += BtnDuality_Click;
            stack.Controls.Add(Row(btnDuality));

            controlsPanel.Controls.Add(stack);

            // Fill-docked control first, then edge-docked controls.
            page.Controls.Add(_rtbSensitivity);
            page.Controls.Add(controlsPanel);

            return page;
        }

        private static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 4)
            };
            foreach (var c in controls)
            {
                c.Margin = new Padding(4, 4, 4, 4);
                row.Controls.Add(c);
            }
            return row;
        }

        private Label SectionHeader(string text) => new Label
        {
            Text = text,
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 14, 0, 4)
        };

        private void RefreshSensitivityDropdowns()
        {
            _cmbVarColumn.Items.Clear();
            _cmbConstraintRow.Items.Clear();
            if (_sf == null) return;

            for (int j = 0; j < _sf.NumCols; j++)
                _cmbVarColumn.Items.Add($"{j}: {_sf.ColNames[j]}");
            if (_cmbVarColumn.Items.Count > 0) _cmbVarColumn.SelectedIndex = 0;

            for (int i = 0; i < _sf.NumRows; i++)
                _cmbConstraintRow.Items.Add($"Constraint {i + 1}");
            if (_cmbConstraintRow.Items.Count > 0) _cmbConstraintRow.SelectedIndex = 0;
        }

        private void AppendSensitivity(string text)
        {
            _rtbSensitivity.AppendText(text.TrimEnd() + Environment.NewLine + Environment.NewLine);
            _rtbSensitivity.SelectionStart = _rtbSensitivity.TextLength;
            _rtbSensitivity.ScrollToCaret();
        }

        /// <summary>Ensures a model is loaded and _primalResult holds an optimal solve, solving on
        /// demand if needed (mirrors the console app's SensitivityMenu behaviour).</summary>
        private bool EnsurePrimalSolved()
        {
            if (!RequireModel()) return false;
            try
            {
                if (_primalResult == null) _primalResult = PrimalSimplex.Solve(_sf!);
            }
            catch (Exception ex) { ShowError(ex); return false; }

            if (_primalResult.Status != SolveStatus.Optimal)
            {
                AppendSensitivity($"Cannot run sensitivity analysis: last solve status was {_primalResult.Status}.");
                return false;
            }
            return true;
        }

        private void BtnSensitivitySolve_Click(object? sender, EventArgs e)
        {
            if (!RequireModel()) return;
            try
            {
                _primalResult = PrimalSimplex.Solve(_sf!);
                RefreshSensitivityDropdowns();
                AppendSensitivity($"Solved. Status: {_primalResult.Status}" +
                    (_primalResult.Status == SolveStatus.Optimal ? $", Z = {_primalResult.ObjectiveValue:F3}" : "."));
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnShadowPrices_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            try
            {
                var y = SensitivityAnalysis.ShadowPrices(_primalResult!, _sf!);
                var sb = new StringBuilder("Shadow prices:").AppendLine();
                for (int i = 0; i < y.Length; i++)
                    sb.AppendLine($"  Constraint {i + 1}: shadow price = {y[i]:F3}");
                AppendSensitivity(sb.ToString());
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnVarRange_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            if (_cmbVarColumn.SelectedIndex < 0) { MessageBox.Show(this, "Select a column first."); return; }
            int col = _cmbVarColumn.SelectedIndex;
            try
            {
                var range = SensitivityAnalysis.RangeOfVariable(_primalResult!, _sf!, col);
                bool basic = _primalResult!.FinalBasis.Contains(col);
                AppendSensitivity($"{_sf!.ColNames[col]} is {(basic ? "BASIC" : "NON-BASIC")}. " +
                                   $"Allowable range for its objective coefficient: {range}");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnApplyCoeff_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            if (_cmbVarColumn.SelectedIndex < 0) { MessageBox.Show(this, "Select a column first."); return; }
            int col = _cmbVarColumn.SelectedIndex;
            if (!double.TryParse(_txtNewCoeffValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double newVal))
            {
                MessageBox.Show(this, "Enter a valid number for the new coefficient.");
                return;
            }
            try
            {
                var range = SensitivityAnalysis.RangeOfVariable(_primalResult!, _sf!, col);
                bool withinRange = (range.LowerIsInfinite || newVal >= range.Lower) &&
                                    (range.UpperIsInfinite || newVal <= range.Upper);
                AppendSensitivity(withinRange
                    ? $"{newVal:F3} is within the allowable range {range} for {_sf!.ColNames[col]} -- the current basis stays optimal."
                    : $"{newVal:F3} is OUTSIDE the allowable range {range} for {_sf!.ColNames[col]} -- re-solving is required to find the new optimum.");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnRhsRange_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            if (_cmbConstraintRow.SelectedIndex < 0) { MessageBox.Show(this, "Select a constraint first."); return; }
            int row = _cmbConstraintRow.SelectedIndex;
            try
            {
                var range = SensitivityAnalysis.RangeRhs(_primalResult!, _sf!, row);
                AppendSensitivity($"Allowable RHS range for constraint {row + 1}: {range}");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnApplyRhs_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            if (_cmbConstraintRow.SelectedIndex < 0) { MessageBox.Show(this, "Select a constraint first."); return; }
            int row = _cmbConstraintRow.SelectedIndex;
            if (!double.TryParse(_txtNewRhsValue.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double newRhs))
            {
                MessageBox.Show(this, "Enter a valid number for the new RHS.");
                return;
            }
            try
            {
                var (_, newObj, feasible) = SensitivityAnalysis.ApplyRhsChange(_primalResult!, _sf!, row, newRhs);
                AppendSensitivity(feasible
                    ? $"Constraint {row + 1} RHS -> {newRhs:F3}: still feasible. New objective value = {newObj:F3}."
                    : $"Constraint {row + 1} RHS -> {newRhs:F3}: this makes the current basis INFEASIBLE " +
                      "(a basic variable would go negative); re-solving is required.");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnNewActivity_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            if (!double.TryParse(_txtNewActivityObj.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double obj))
            {
                MessageBox.Show(this, "Enter a valid objective coefficient.");
                return;
            }

            double[] col;
            try
            {
                col = ParseDoubleList(_txtNewActivityCoeffs.Text, _sf!.NumRows,
                    "the new activity's constraint column (one value per constraint)");
            }
            catch (Exception ex) { ShowError(ex); return; }

            try
            {
                var (reduced, wouldImprove) = SensitivityAnalysis.EvaluateNewActivity(_primalResult!, _sf!, obj, col);
                AppendSensitivity($"Priced-out reduced cost of the new activity: {reduced:F3}" + Environment.NewLine +
                    (wouldImprove
                        ? "This activity WOULD improve the solution -- add it to the model and re-solve."
                        : "This activity would NOT improve the solution -- the current optimum stays optimal."));
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnNewConstraint_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;

            double[] coeffs;
            try
            {
                coeffs = ParseDoubleList(_txtNewConstraintCoeffs.Text, _model!.NumVars,
                    "the new constraint's coefficients (one value per variable)");
            }
            catch (Exception ex) { ShowError(ex); return; }

            if (!double.TryParse(_txtNewConstraintRhs.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double rhs))
            {
                MessageBox.Show(this, "Enter a valid RHS value.");
                return;
            }
            var relation = _cmbRelation.SelectedItem?.ToString() switch
            {
                ">=" => Relation.GreaterOrEqual,
                "=" => Relation.Equal,
                _ => Relation.LessOrEqual
            };

            try
            {
                var (lhs, ok) = SensitivityAnalysis.EvaluateNewConstraint(_primalResult!, coeffs, relation, rhs);
                AppendSensitivity($"Current solution gives LHS = {lhs:F3}." + Environment.NewLine +
                    (ok
                        ? "The current optimal solution already satisfies this constraint -- it stays optimal."
                        : "The current optimal solution VIOLATES this constraint -- add it to the input file and re-solve."));
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnDuality_Click(object? sender, EventArgs e)
        {
            if (!EnsurePrimalSolved()) return;
            try
            {
                var dualModel = Duality.BuildDual(_model!);
                var dualSf = StandardForm.Build(dualModel);
                var dualResult = PrimalSimplex.Solve(dualSf);

                var sb = new StringBuilder();
                sb.Append(OutputWriter.FormatCanonicalForm(dualSf));
                sb.AppendLine($"Dual status: {dualResult.Status}");
                if (dualResult.Status == SolveStatus.Optimal)
                {
                    sb.AppendLine($"Dual optimal Z = {dualResult.ObjectiveValue:F3}");
                    for (int i = 0; i < dualModel.NumVars; i++)
                        sb.AppendLine($"  y{i + 1} = {dualResult.OriginalSolution[i]:F3}");
                    sb.AppendLine(Duality.CheckDuality(_primalResult!.ObjectiveValue, dualResult.ObjectiveValue));
                }
                else
                {
                    sb.AppendLine(dualResult.Message);
                }
                AppendSensitivity(sb.ToString());
            }
            catch (Exception ex) { ShowError(ex); }
        }
    }
}
