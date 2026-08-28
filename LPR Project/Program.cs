using System;
using System.Windows.Forms;
using LPR381Solver.Forms;

namespace LPR381Solver
{
    internal static class AppEntry
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
