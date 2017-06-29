﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkProject
{
    public partial class WinningForm : Form
    {
        public WinningForm(int playerNumber)
        {
            InitializeComponent();
            Winner.Text += "" + playerNumber;
        }

        private void WinningForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
    }
}
