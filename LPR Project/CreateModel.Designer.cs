namespace LPR_Project
{
    partial class CreateModel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.label1 = new System.Windows.Forms.Label();
            this.radMax = new System.Windows.Forms.RadioButton();
            this.radMin = new System.Windows.Forms.RadioButton();
            this.btnAddObjFunc = new System.Windows.Forms.Button();
            this.txtDecisionVariables = new System.Windows.Forms.TextBox();
            this.grpProblem = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblObjFunc = new System.Windows.Forms.Label();
            this.dgvConstraints = new System.Windows.Forms.DataGridView();
            this.cmbRelation = new System.Windows.Forms.ComboBox();
            this.txtLHS = new System.Windows.Forms.TextBox();
            this.txtRHS = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.btnAddConstraint = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btnFinish = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.grpProblem.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConstraints)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(69, 244);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Decision Variables:";
            // 
            // radMax
            // 
            this.radMax.AutoSize = true;
            this.radMax.Location = new System.Drawing.Point(33, 28);
            this.radMax.Name = "radMax";
            this.radMax.Size = new System.Drawing.Size(45, 17);
            this.radMax.TabIndex = 1;
            this.radMax.TabStop = true;
            this.radMax.Text = "Max";
            this.radMax.UseVisualStyleBackColor = true;
            // 
            // radMin
            // 
            this.radMin.AutoSize = true;
            this.radMin.Location = new System.Drawing.Point(33, 62);
            this.radMin.Name = "radMin";
            this.radMin.Size = new System.Drawing.Size(42, 17);
            this.radMin.TabIndex = 2;
            this.radMin.TabStop = true;
            this.radMin.Text = "Min";
            this.radMin.UseVisualStyleBackColor = true;
            // 
            // btnAddObjFunc
            // 
            this.btnAddObjFunc.Location = new System.Drawing.Point(104, 295);
            this.btnAddObjFunc.Name = "btnAddObjFunc";
            this.btnAddObjFunc.Size = new System.Drawing.Size(146, 23);
            this.btnAddObjFunc.TabIndex = 3;
            this.btnAddObjFunc.Text = "Add Objective Function";
            this.btnAddObjFunc.UseVisualStyleBackColor = true;
            // 
            // txtDecisionVariables
            // 
            this.txtDecisionVariables.Location = new System.Drawing.Point(189, 241);
            this.txtDecisionVariables.Name = "txtDecisionVariables";
            this.txtDecisionVariables.Size = new System.Drawing.Size(100, 20);
            this.txtDecisionVariables.TabIndex = 4;
            // 
            // grpProblem
            // 
            this.grpProblem.Controls.Add(this.radMin);
            this.grpProblem.Controls.Add(this.radMax);
            this.grpProblem.Location = new System.Drawing.Point(115, 108);
            this.grpProblem.Name = "grpProblem";
            this.grpProblem.Size = new System.Drawing.Size(119, 100);
            this.grpProblem.TabIndex = 5;
            this.grpProblem.TabStop = false;
            this.grpProblem.Text = "Problem";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(67, 352);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Objective Function:";
            // 
            // lblObjFunc
            // 
            this.lblObjFunc.AutoSize = true;
            this.lblObjFunc.Location = new System.Drawing.Point(190, 352);
            this.lblObjFunc.Name = "lblObjFunc";
            this.lblObjFunc.Size = new System.Drawing.Size(94, 13);
            this.lblObjFunc.TabIndex = 7;
            this.lblObjFunc.Text = "*obj function here*";
            // 
            // dgvConstraints
            // 
            this.dgvConstraints.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConstraints.Location = new System.Drawing.Point(429, 244);
            this.dgvConstraints.Name = "dgvConstraints";
            this.dgvConstraints.Size = new System.Drawing.Size(288, 150);
            this.dgvConstraints.TabIndex = 8;
            // 
            // cmbRelation
            // 
            this.cmbRelation.FormattingEnabled = true;
            this.cmbRelation.Items.AddRange(new object[] {
            "<=",
            ">=",
            "="});
            this.cmbRelation.Location = new System.Drawing.Point(533, 136);
            this.cmbRelation.Name = "cmbRelation";
            this.cmbRelation.Size = new System.Drawing.Size(79, 21);
            this.cmbRelation.TabIndex = 9;
            // 
            // txtLHS
            // 
            this.txtLHS.Location = new System.Drawing.Point(403, 137);
            this.txtLHS.Name = "txtLHS";
            this.txtLHS.Size = new System.Drawing.Size(100, 20);
            this.txtLHS.TabIndex = 10;
            // 
            // txtRHS
            // 
            this.txtRHS.Location = new System.Drawing.Point(641, 137);
            this.txtRHS.Name = "txtRHS";
            this.txtRHS.Size = new System.Drawing.Size(100, 20);
            this.txtRHS.TabIndex = 11;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(400, 108);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "LHS:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(638, 108);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "RHS:";
            // 
            // btnAddConstraint
            // 
            this.btnAddConstraint.Location = new System.Drawing.Point(517, 197);
            this.btnAddConstraint.Name = "btnAddConstraint";
            this.btnAddConstraint.Size = new System.Drawing.Size(115, 23);
            this.btnAddConstraint.TabIndex = 14;
            this.btnAddConstraint.Text = "Add Constraint";
            this.btnAddConstraint.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(81, 45);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(191, 25);
            this.label5.TabIndex = 15;
            this.label5.Text = "Objective Function";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(511, 45);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(121, 25);
            this.label6.TabIndex = 16;
            this.label6.Text = "Constraints";
            // 
            // btnFinish
            // 
            this.btnFinish.Location = new System.Drawing.Point(644, 415);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new System.Drawing.Size(73, 23);
            this.btnFinish.TabIndex = 17;
            this.btnFinish.Text = "Finish";
            this.btnFinish.UseVisualStyleBackColor = true;
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(12, 12);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(73, 23);
            this.btnBack.TabIndex = 18;
            this.btnBack.Text = "Back";
            this.btnBack.UseVisualStyleBackColor = true;
            // 
            // CreateModel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 450);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnFinish);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btnAddConstraint);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtRHS);
            this.Controls.Add(this.txtLHS);
            this.Controls.Add(this.cmbRelation);
            this.Controls.Add(this.dgvConstraints);
            this.Controls.Add(this.lblObjFunc);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.grpProblem);
            this.Controls.Add(this.txtDecisionVariables);
            this.Controls.Add(this.btnAddObjFunc);
            this.Controls.Add(this.label1);
            this.Name = "CreateModel";
            this.Text = "Create Model";
            this.grpProblem.ResumeLayout(false);
            this.grpProblem.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConstraints)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radMax;
        private System.Windows.Forms.RadioButton radMin;
        private System.Windows.Forms.Button btnAddObjFunc;
        private System.Windows.Forms.TextBox txtDecisionVariables;
        private System.Windows.Forms.GroupBox grpProblem;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblObjFunc;
        private System.Windows.Forms.DataGridView dgvConstraints;
        private System.Windows.Forms.ComboBox cmbRelation;
        private System.Windows.Forms.TextBox txtLHS;
        private System.Windows.Forms.TextBox txtRHS;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnAddConstraint;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnFinish;
        private System.Windows.Forms.Button btnBack;
    }
}