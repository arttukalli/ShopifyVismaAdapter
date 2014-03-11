using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Configuration;


namespace ShopifyVismaApp
{
    public partial class Console : Form
    {
        public Console()
        {
            InitializeComponent();
        }

        

        private void RunButton_Click(object sender, EventArgs e)
        {

            int shopID = int.Parse(accountBox.SelectedValue.ToString());
            BackgroundWorker bw = new BackgroundWorker();

            Adapter adapter = new Adapter();
            adapter.Logged += delegate(object adapterSender, Adapter.LoggedEventArgs adaptere)
            { UpdateTextBox(adaptere.text); };
            adapter.StatusUpdated += delegate(object adapterSender, Adapter.StatusUpdatedEventArgs adaptere)
            { UpdateStatus(adaptere.text); };



            bw.DoWork += delegate(object bwSender, DoWorkEventArgs bwe)
            { ((Adapter)bwe.Argument).UpdateRecords(shopID); };
            bw.RunWorkerAsync(adapter);
            //adapter.UpdateRecords(shopID);

            
        }

        /// <summary>
        /// Log to textarea.
        /// </summary>
        /// <param name="text"></param>
        public void UpdateTextBox(string text)
        {
            int maxText = 30000;
            int trimToText = 20000;
           
            // Handle multiple threads
            if (OutputTextBox.InvokeRequired)
            {
                //This techniques is from answer by @sinperX1
                BeginInvoke((MethodInvoker)(() => { UpdateTextBox(text); }));
                return;
            }

            // Trim if text in TextBox is too long to improve performance
            string outputText = OutputTextBox.Text;
            //outputText += text + Environment.NewLine;
            if (outputText.Length > maxText)
                OutputTextBox.Text = OutputTextBox.Text.Substring(outputText.Length - trimToText);

            OutputTextBox.AppendText(text + Environment.NewLine);
        }

        /// <summary>
        /// Log to textarea.
        /// </summary>
        /// <param name="text"></param>
        public void UpdateStatus(string text)
        {
            // Handle multiple threads
            if (StatusLabel.InvokeRequired)
            {
                //This techniques is from answer by @sinperX1
                BeginInvoke((MethodInvoker)(() => { UpdateStatus(text); }));
                return;
            }

            StatusLabel.Text = text;
        }

        private void StopButton_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Console_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'dataSet.Shop' table. You can move, or remove it, as needed.
            this.shopTableAdapter.Fill(this.dataSet.Shop);

        }

    }
           


}
