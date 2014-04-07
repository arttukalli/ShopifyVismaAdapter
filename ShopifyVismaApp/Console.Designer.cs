namespace ShopifyVismaApp
{
    partial class Console
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
            this.components = new System.ComponentModel.Container();
            this.RunButton = new System.Windows.Forms.Button();
            this.OutputTextBox = new System.Windows.Forms.TextBox();
            this.accountBox = new System.Windows.Forms.ComboBox();
            this.shopBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dataSet = new ShopifyVismaApp.DataSet();
            this.label1 = new System.Windows.Forms.Label();
            this.shopTableAdapter = new ShopifyVismaApp.DataSetTableAdapters.ShopTableAdapter();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.checkBoxFullUpdate = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.shopBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).BeginInit();
            this.SuspendLayout();
            // 
            // RunButton
            // 
            this.RunButton.Location = new System.Drawing.Point(598, 12);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(117, 23);
            this.RunButton.TabIndex = 0;
            this.RunButton.Text = "Start";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // OutputTextBox
            // 
            this.OutputTextBox.Location = new System.Drawing.Point(15, 93);
            this.OutputTextBox.Multiline = true;
            this.OutputTextBox.Name = "OutputTextBox";
            this.OutputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.OutputTextBox.Size = new System.Drawing.Size(700, 264);
            this.OutputTextBox.TabIndex = 1;
            // 
            // accountBox
            // 
            this.accountBox.DataSource = this.shopBindingSource;
            this.accountBox.DisplayMember = "ShopifyStoreAccount";
            this.accountBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.accountBox.FormattingEnabled = true;
            this.accountBox.Location = new System.Drawing.Point(96, 14);
            this.accountBox.Name = "accountBox";
            this.accountBox.Size = new System.Drawing.Size(242, 21);
            this.accountBox.TabIndex = 2;
            this.accountBox.ValueMember = "ID";
            // 
            // shopBindingSource
            // 
            this.shopBindingSource.DataMember = "Shop";
            this.shopBindingSource.DataSource = this.dataSet;
            // 
            // dataSet
            // 
            this.dataSet.DataSetName = "DataSet";
            this.dataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Shop Account:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // shopTableAdapter
            // 
            this.shopTableAdapter.ClearBeforeFill = true;
            // 
            // StatusLabel
            // 
            this.StatusLabel.AutoSize = true;
            this.StatusLabel.Location = new System.Drawing.Point(12, 65);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(41, 13);
            this.StatusLabel.TabIndex = 4;
            this.StatusLabel.Text = "Ready.";
            // 
            // checkBoxFullUpdate
            // 
            this.checkBoxFullUpdate.AutoSize = true;
            this.checkBoxFullUpdate.Location = new System.Drawing.Point(357, 17);
            this.checkBoxFullUpdate.Name = "checkBoxFullUpdate";
            this.checkBoxFullUpdate.Size = new System.Drawing.Size(80, 17);
            this.checkBoxFullUpdate.TabIndex = 5;
            this.checkBoxFullUpdate.Text = "Full Update";
            this.checkBoxFullUpdate.UseVisualStyleBackColor = true;
            // 
            // Console
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(732, 369);
            this.Controls.Add(this.checkBoxFullUpdate);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.accountBox);
            this.Controls.Add(this.OutputTextBox);
            this.Controls.Add(this.RunButton);
            this.Name = "Console";
            this.Text = "Shopify Visma Adapter ";
            this.Load += new System.EventHandler(this.Console_Load);
            ((System.ComponentModel.ISupportInitialize)(this.shopBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.TextBox OutputTextBox;
        private System.Windows.Forms.ComboBox accountBox;
        private System.Windows.Forms.Label label1;
        private DataSet dataSet;
        private System.Windows.Forms.BindingSource shopBindingSource;
        private DataSetTableAdapters.ShopTableAdapter shopTableAdapter;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.CheckBox checkBoxFullUpdate;
    }
}

