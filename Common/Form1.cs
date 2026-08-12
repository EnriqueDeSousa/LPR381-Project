using System;
using System.Windows.Forms;
using LPR381Project.Common.Errors;

namespace LPR381Project
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void RunSolver(Action solver)
        {
            try
            {
                solver();
            }
            catch (SolverException exception)
            {
                ErrorHandler.Handle(exception);

                MessageBox.Show(
                    ErrorHandler.GetMessage(exception),
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (Exception exception)
            {
                ErrorHandler.Handle(exception);

                MessageBox.Show(
                    "An unexpected error occurred. Please try again.",
                    "Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}