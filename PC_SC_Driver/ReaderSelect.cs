using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CustomMifareReader
{
    public partial class ReaderSelect : Form
    {
        public string CurrentReader = "";

        public ReaderSelect()
        {
            InitializeComponent();
        }

        public void Init(string[] readers)
        {
            comboBox1.Items.AddRange(readers);
            comboBox1.SelectedIndex = comboBox1.Items.IndexOf(readers.FirstOrDefault(t => t.Contains("CL")) ?? readers.FirstOrDefault(t => t.Contains("PICC")));

            //TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!
            //button_select.PerformClick();
            //button_select_Click(null,null);
        }

        private void button_select_Click(object sender, EventArgs e)
        {
            CurrentReader = comboBox1.SelectedItem.ToString();
        }
    }
}
