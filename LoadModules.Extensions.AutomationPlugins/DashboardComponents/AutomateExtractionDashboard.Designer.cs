namespace LoadModules.Extensions.AutomationPlugins.DashboardComponents
{
    partial class AutomateExtractionDashboard
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCreateAutomationDatabase = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCreateAutomationDatabase
            // 
            this.btnCreateAutomationDatabase.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCreateAutomationDatabase.Location = new System.Drawing.Point(226, 191);
            this.btnCreateAutomationDatabase.Name = "btnCreateAutomationDatabase";
            this.btnCreateAutomationDatabase.Size = new System.Drawing.Size(177, 23);
            this.btnCreateAutomationDatabase.TabIndex = 0;
            this.btnCreateAutomationDatabase.Text = "Create Automation Database";
            this.btnCreateAutomationDatabase.UseVisualStyleBackColor = true;
            this.btnCreateAutomationDatabase.Click += new System.EventHandler(this.btnCreateAutomationDatabase_Click);
            // 
            // AutomateExtractionDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnCreateAutomationDatabase);
            this.Name = "AutomateExtractionDashboard";
            this.Size = new System.Drawing.Size(641, 453);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCreateAutomationDatabase;
    }
}
