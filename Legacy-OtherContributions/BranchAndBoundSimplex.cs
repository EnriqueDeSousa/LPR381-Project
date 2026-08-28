using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace LPR_Project
{
    public class BranchAndBoundSimplex : Form
    {
        private NumericUpDown numVars;
        private NumericUpDown numConstraints;
        private Button btnGenerate;
        private Button btnSolve;
        private CheckBox chkVerbose;

        private Label lblObjective;
        private DataGridView dgvObjective;

        private Label lblIntegers;
        private FlowLayoutPanel pnlIntegerChecks;

        private Label lblConstraints;
        private DataGridView dgvConstraints;

        private Label lblOutput;
        private RichTextBox rtbOutput;

        private List<CheckBox> _integerCheckBoxes;

        public BranchAndBoundSimplex()
        {
            _integerCheckBoxes = new List<CheckBox>();
            InitializeComponent();
            GenerateGrids(2, 2);
        }

        private void InitializeComponent()
        {
            Text = "Branch and Bound Simplex Solver";
            Width = 1100;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            Label lblNumVars = new Label();
            lblNumVars.Text = "Number of variables:";
            lblNumVars.Location = new Point(12, 15);
            lblNumVars.AutoSize = true;

            numVars = new NumericUpDown();
            numVars.Location = new Point(150, 12);
            numVars.Minimum = 1;
            numVars.Maximum = 10;
            numVars.Value = 2;
            numVars.Width = 60;

            Label lblNumConstraints = new Label();
            lblNumConstraints.Text = "Number of constraints:";
            lblNumConstraints.Location = new Point(230, 15);
            lblNumConstraints.AutoSize = true;

            numConstraints = new NumericUpDown();
            numConstraints.Location = new Point(380, 12);
            numConstraints.Minimum = 1;
            numConstraints.Maximum = 15;
            numConstraints.Value = 2;
            numConstraints.Width = 60;

            btnGenerate = new Button();
            btnGenerate.Text = "Generate Grids";
            btnGenerate.Location = new Point(460, 10);
            btnGenerate.Width = 120;
            btnGenerate.Click += BtnGenerate_Click;

            chkVerbose = new CheckBox();
            chkVerbose.Text = "Show full simplex tableaus";
            chkVerbose.Location = new Point(600, 14);
            chkVerbose.AutoSize = true;

            btnSolve = new Button();
            btnSolve.Text = "Solve (Maximize)";
            btnSolve.Location = new Point(820, 10);
            btnSolve.Width = 150;
            btnSolve.Click += BtnSolve_Click;

            lblObjective = new Label();
            lblObjective.Text = "Objective coefficients (maximize c1*x1 + c2*x2 + ...):";
            lblObjective.Location = new Point(12, 50);
            lblObjective.AutoSize = true;

            dgvObjective = new DataGridView();
            dgvObjective.Location = new Point(12, 72);
            dgvObjective.Width = 1060;
            dgvObjective.Height = 60;
            dgvObjective.RowHeadersVisible = false;
            dgvObjective.AllowUserToAddRows = false;
            dgvObjective.AllowUserToDeleteRows = false;
            dgvObjective.AllowUserToResizeRows = false;
            dgvObjective.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblIntegers = new Label();
            lblIntegers.Text = "Variables that must be integer:";
            lblIntegers.Location = new Point(12, 140);
            lblIntegers.AutoSize = true;

            pnlIntegerChecks = new FlowLayoutPanel();
            pnlIntegerChecks.Location = new Point(12, 160);
            pnlIntegerChecks.Width = 1060;
            pnlIntegerChecks.Height = 30;
            pnlIntegerChecks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblConstraints = new Label();
            lblConstraints.Text = "Constraints (each row: coefficients, relation, right-hand side):";
            lblConstraints.Location = new Point(12, 198);
            lblConstraints.AutoSize = true;

            dgvConstraints = new DataGridView();
            dgvConstraints.Location = new Point(12, 220);
            dgvConstraints.Width = 1060;
            dgvConstraints.Height = 180;
            dgvConstraints.RowHeadersVisible = false;
            dgvConstraints.AllowUserToAddRows = false;
            dgvConstraints.AllowUserToDeleteRows = false;
            dgvConstraints.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblOutput = new Label();
            lblOutput.Text = "Solver log (simplex tableaus + branch-and-bound tree):";
            lblOutput.Location = new Point(12, 410);
            lblOutput.AutoSize = true;

            rtbOutput = new RichTextBox();
            rtbOutput.Location = new Point(12, 432);
            rtbOutput.Width = 1060;
            rtbOutput.Height = 320;
            rtbOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbOutput.ReadOnly = true;
            rtbOutput.WordWrap = false;
            rtbOutput.ScrollBars = RichTextBoxScrollBars.Both;
            rtbOutput.Font = new Font("Consolas", 9F);

            Controls.Add(lblNumVars);
            Controls.Add(numVars);
            Controls.Add(lblNumConstraints);
            Controls.Add(numConstraints);
            Controls.Add(btnGenerate);
            Controls.Add(chkVerbose);
            Controls.Add(btnSolve);
            Controls.Add(lblObjective);
            Controls.Add(dgvObjective);
            Controls.Add(lblIntegers);
            Controls.Add(pnlIntegerChecks);
            Controls.Add(lblConstraints);
            Controls.Add(dgvConstraints);
            Controls.Add(lblOutput);
            Controls.Add(rtbOutput);
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            GenerateGrids((int)numVars.Value, (int)numConstraints.Value);
        }

        private void GenerateGrids(int n, int m)
        {
            // --- Objective grid: one row, one numeric column per variable ---
            dgvObjective.Columns.Clear();
            dgvObjective.Rows.Clear();
            for (int j = 0; j < n; j++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.Name = "x" + (j + 1);
                col.HeaderText = "x" + (j + 1);
                dgvObjective.Columns.Add(col);
            }
            dgvObjective.Rows.Add();
            for (int j = 0; j < n; j++)
                dgvObjective.Rows[0].Cells[j].Value = "0";

            // --- Integer checkboxes, one per variable ---
            pnlIntegerChecks.Controls.Clear();
            _integerCheckBoxes.Clear();
            for (int j = 0; j < n; j++)
            {
                CheckBox cb = new CheckBox();
                cb.Text = "x" + (j + 1);
                cb.Checked = true;
                cb.AutoSize = true;
                cb.Margin = new Padding(8, 3, 8, 3);
                pnlIntegerChecks.Controls.Add(cb);
                _integerCheckBoxes.Add(cb);
            }

            // --- Constraints grid: n coefficient columns + relation + RHS ---
            dgvConstraints.Columns.Clear();
            dgvConstraints.Rows.Clear();
            for (int j = 0; j < n; j++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.Name = "x" + (j + 1);
                col.HeaderText = "x" + (j + 1);
                dgvConstraints.Columns.Add(col);
            }
            DataGridViewComboBoxColumn relCol = new DataGridViewComboBoxColumn();
            relCol.Name = "Relation";
            relCol.HeaderText = "Relation";
            relCol.Items.AddRange(new object[] { "<=", ">=", "=" });
            relCol.Width = 70;
            dgvConstraints.Columns.Add(relCol);

            DataGridViewTextBoxColumn rhsCol = new DataGridViewTextBoxColumn();
            rhsCol.Name = "RHS";
            rhsCol.HeaderText = "RHS";
            dgvConstraints.Columns.Add(rhsCol);

            for (int i = 0; i < m; i++)
            {
                int rowIndex = dgvConstraints.Rows.Add();
                for (int j = 0; j < n; j++)
                    dgvConstraints.Rows[rowIndex].Cells[j].Value = "0";
                dgvConstraints.Rows[rowIndex].Cells["Relation"].Value = "<=";
                dgvConstraints.Rows[rowIndex].Cells["RHS"].Value = "0";
            }
        }

        private void BtnSolve_Click(object sender, EventArgs e)
        {
            rtbOutput.Clear();
            try
            {
                LinearProgram lp = BuildLinearProgramFromGrids();
                List<int> integerIndices = new List<int>();
                for (int j = 0; j < _integerCheckBoxes.Count; j++)
                {
                    if (_integerCheckBoxes[j].Checked) integerIndices.Add(j);
                }

                Action<string> logger = delegate (string line)
                {
                    rtbOutput.AppendText(line + Environment.NewLine);
                };

                logger("Solving:");
                logger("  maximize " + DescribeObjective(lp));
                for (int i = 0; i < lp.Constraints.Count; i++)
                    logger("  s.t. " + DescribeConstraint(lp, i));
                logger("  Integer variables: " + (integerIndices.Count > 0 ? string.Join(", ", integerIndices.ConvertAll(idx => lp.VariableNames[idx])) : "(none)"));

                BranchAndBoundSolver solver = new BranchAndBoundSolver(integerIndices, chkVerbose.Checked, logger);
                BranchAndBoundResult result = solver.Solve(lp);

                logger("");
                logger("=====================================");
                if (result.Found)
                {
                    logger("Optimal objective: " + result.Objective.ToString("F4"));
                    for (int i = 0; i < result.Solution.Length; i++)
                        logger("  " + lp.VariableNames[i] + " = " + result.Solution[i].ToString("F4"));
                }
                else
                {
                    logger("No feasible solution was found.");
                }
                logger("Nodes explored: " + result.NodesExplored);
                logger("=====================================");

                rtbOutput.SelectionStart = 0;
                rtbOutput.ScrollToCaret();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private LinearProgram BuildLinearProgramFromGrids()
        {
            int n = dgvObjective.Columns.Count;
            double[] objCoeffs = new double[n];
            for (int j = 0; j < n; j++)
                objCoeffs[j] = ParseCell(dgvObjective.Rows[0].Cells[j].Value, "Objective coefficient x" + (j + 1));

            string[] names = new string[n];
            for (int j = 0; j < n; j++) names[j] = "x" + (j + 1);

            LinearProgram lp = new LinearProgram(objCoeffs, names);

            for (int i = 0; i < dgvConstraints.Rows.Count; i++)
            {
                double[] coeffs = new double[n];
                for (int j = 0; j < n; j++)
                    coeffs[j] = ParseCell(dgvConstraints.Rows[i].Cells[j].Value, "Constraint " + (i + 1) + ", coefficient x" + (j + 1));

                object relValue = dgvConstraints.Rows[i].Cells["Relation"].Value;
                string relText = relValue != null ? relValue.ToString() : "<=";
                Relation relation;
                if (relText == "<=") relation = Relation.LE;
                else if (relText == ">=") relation = Relation.GE;
                else relation = Relation.EQ;

                double rhs = ParseCell(dgvConstraints.Rows[i].Cells["RHS"].Value, "Constraint " + (i + 1) + " RHS");

                lp.AddConstraint(coeffs, relation, rhs);
            }

            return lp;
        }

        private double ParseCell(object cellValue, string fieldDescription)
        {
            if (cellValue == null || cellValue.ToString().Trim().Length == 0)
                return 0.0;

            double value;
            if (!double.TryParse(cellValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new FormatException("Invalid number for " + fieldDescription + ": '" + cellValue + "'");
            return value;
        }

        private string DescribeObjective(LinearProgram lp)
        {
            List<string> parts = new List<string>();
            for (int j = 0; j < lp.NumVariables; j++)
                parts.Add(lp.ObjectiveCoefficients[j].ToString("F2") + lp.VariableNames[j]);
            return string.Join(" + ", parts);
        }

        private string DescribeConstraint(LinearProgram lp, int index)
        {
            Constraint c = lp.Constraints[index];
            List<string> parts = new List<string>();
            for (int j = 0; j < lp.NumVariables; j++)
                parts.Add(c.Coefficients[j].ToString("F2") + lp.VariableNames[j]);
            string relText = c.Relation == Relation.LE ? "<=" : (c.Relation == Relation.GE ? ">=" : "=");
            return string.Join(" + ", parts) + " " + relText + " " + c.Rhs.ToString("F2");
        }
    }
}