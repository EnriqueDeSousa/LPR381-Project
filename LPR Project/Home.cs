using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPR_Project
{
    public partial class Home : Form
    {
        public Home()
        {
            InitializeComponent();
        }

        BranchAndBoundSimplex branchAndBoundSimplex = new BranchAndBoundSimplex();

        private void btnCreateNewModel_Click(object sender, EventArgs e)
        {
            branchAndBoundSimplex.Show();
        }
    }
}
