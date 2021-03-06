﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.DBXLua
{
    public partial class ScriptWarningForm : Form
    {
        public ScriptWarningForm()
        {
            InitializeComponent();
        }

        // returns true if proceed
        // return false if undo
        public static bool AskUndo(string text)
        {
            ScriptWarningForm box = new ScriptWarningForm();
            box.acceptButton.Focus();
            box.acceptButton.Select();
            // i hate this, it's ugly, but it's too much of a pain to do it otherwise
            text = text.Replace("\n", Environment.NewLine);
            box.warningTextBox.Clear();
            box.warningTextBox.AppendText(text);
            
            return box.ShowDialog() == DialogResult.Cancel;
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
