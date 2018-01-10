using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImageCatcher
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ScreenForm screen = new ScreenForm();
            //screen.copytoFather += new copyToFatherTextBox(copytoTextBox);
            screen.ShowDialog();
        }
    }
}
