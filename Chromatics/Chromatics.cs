using Chromatics.Forms;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics
{
    public partial class Chromatics : MetroForm
    {
        public Chromatics()
        {
            InitializeComponent();
            var mainWindow = new Fm_MainWindow();
            mainWindow.Show();
            this.Hide();
        }

    }
}
