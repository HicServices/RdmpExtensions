namespace LoadModules.Extensions.AutomationPluginsUIs.Dialogs
{
    partial class EnqueueExtractionConfigurationUI
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
            this.pbExtractionConfiguration = new System.Windows.Forms.PictureBox();
            this.lblExtractionConfigurationName = new System.Windows.Forms.Label();
            this.pPipeline = new System.Windows.Forms.Panel();
            this.ddTimescale = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnQueExtraction = new System.Windows.Forms.Button();
            this.ddDay = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbExtractionConfiguration)).BeginInit();
            this.SuspendLayout();
            // 
            // pbExtractionConfiguration
            // 
            this.pbExtractionConfiguration.Location = new System.Drawing.Point(12, 12);
            this.pbExtractionConfiguration.Name = "pbExtractionConfiguration";
            this.pbExtractionConfiguration.Size = new System.Drawing.Size(26, 26);
            this.pbExtractionConfiguration.TabIndex = 0;
            this.pbExtractionConfiguration.TabStop = false;
            // 
            // lblExtractionConfigurationName
            // 
            this.lblExtractionConfigurationName.AutoSize = true;
            this.lblExtractionConfigurationName.Location = new System.Drawing.Point(40, 19);
            this.lblExtractionConfigurationName.Name = "lblExtractionConfigurationName";
            this.lblExtractionConfigurationName.Size = new System.Drawing.Size(35, 13);
            this.lblExtractionConfigurationName.TabIndex = 1;
            this.lblExtractionConfigurationName.Text = "label1";
            // 
            // pPipeline
            // 
            this.pPipeline.Location = new System.Drawing.Point(12, 44);
            this.pPipeline.Name = "pPipeline";
            this.pPipeline.Size = new System.Drawing.Size(712, 146);
            this.pPipeline.TabIndex = 5;
            // 
            // ddTimescale
            // 
            this.ddTimescale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddTimescale.FormattingEnabled = true;
            this.ddTimescale.Location = new System.Drawing.Point(77, 194);
            this.ddTimescale.Name = "ddTimescale";
            this.ddTimescale.Size = new System.Drawing.Size(120, 21);
            this.ddTimescale.TabIndex = 6;
            this.ddTimescale.SelectedIndexChanged += new System.EventHandler(this.ddTimescale_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 197);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Timescale:";
            // 
            // btnQueExtraction
            // 
            this.btnQueExtraction.Location = new System.Drawing.Point(77, 221);
            this.btnQueExtraction.Name = "btnQueExtraction";
            this.btnQueExtraction.Size = new System.Drawing.Size(120, 23);
            this.btnQueExtraction.TabIndex = 8;
            this.btnQueExtraction.Text = "Queue Extraction";
            this.btnQueExtraction.UseVisualStyleBackColor = true;
            this.btnQueExtraction.Click += new System.EventHandler(this.btnQueExtraction_Click);
            // 
            // ddDay
            // 
            this.ddDay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddDay.FormattingEnabled = true;
            this.ddDay.Location = new System.Drawing.Point(203, 194);
            this.ddDay.Name = "ddDay";
            this.ddDay.Size = new System.Drawing.Size(120, 21);
            this.ddDay.TabIndex = 6;
            this.ddDay.SelectedIndexChanged += new System.EventHandler(this.ddTimescale_SelectedIndexChanged);
            // 
            // EnqueueExtractionConfigurationUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(733, 251);
            this.Controls.Add(this.btnQueExtraction);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ddDay);
            this.Controls.Add(this.ddTimescale);
            this.Controls.Add(this.pPipeline);
            this.Controls.Add(this.lblExtractionConfigurationName);
            this.Controls.Add(this.pbExtractionConfiguration);
            this.Name = "EnqueueExtractionConfigurationUI";
            this.Text = "EnqueueExtractionConfigurationUI";
            ((System.ComponentModel.ISupportInitialize)(this.pbExtractionConfiguration)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbExtractionConfiguration;
        private System.Windows.Forms.Label lblExtractionConfigurationName;
        private System.Windows.Forms.Panel pPipeline;
        private System.Windows.Forms.ComboBox ddTimescale;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnQueExtraction;
        private System.Windows.Forms.ComboBox ddDay;
    }
}