namespace LPR_Project
{
    partial class Home
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnCreateNewModel = new System.Windows.Forms.Button();
            this.btnViewPreviousModel = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(125, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(281, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "Linear Programming App";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(146, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(235, 24);
            this.label2.TabIndex = 1;
            this.label2.Text = "What would you like to do?";
            // 
            // btnCreateNewModel
            // 
            this.btnCreateNewModel.Location = new System.Drawing.Point(202, 211);
            this.btnCreateNewModel.Name = "btnCreateNewModel";
            this.btnCreateNewModel.Size = new System.Drawing.Size(121, 23);
            this.btnCreateNewModel.TabIndex = 2;
            this.btnCreateNewModel.Text = "Create New Model";
            this.btnCreateNewModel.UseVisualStyleBackColor = true;
            this.btnCreateNewModel.Click += new System.EventHandler(this.btnCreateNewModel_Click);
            // 
            // btnViewPreviousModel
            // 
            this.btnViewPreviousModel.Location = new System.Drawing.Point(202, 270);
            this.btnViewPreviousModel.Name = "btnViewPreviousModel";
            this.btnViewPreviousModel.Size = new System.Drawing.Size(121, 23);
            this.btnViewPreviousModel.TabIndex = 3;
            this.btnViewPreviousModel.Text = "View Previous Model";
            this.btnViewPreviousModel.UseVisualStyleBackColor = true;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(202, 328);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(121, 23);
            this.btnExit.TabIndex = 4;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            // 
            // Home
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(543, 450);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnViewPreviousModel);
            this.Controls.Add(this.btnCreateNewModel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Home";
            this.Text = "Home";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnCreateNewModel;
        private System.Windows.Forms.Button btnViewPreviousModel;
        private System.Windows.Forms.Button btnExit;
    }
}

