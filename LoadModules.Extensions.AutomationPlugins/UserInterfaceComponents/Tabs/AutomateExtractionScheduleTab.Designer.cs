using CatalogueManager.LocationsMenu.Ticketing;
using CatalogueManager.SimpleControls;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents.Tabs
{
    partial class AutomateExtractionScheduleTab
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
            this.lblName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbComment = new System.Windows.Forms.TextBox();
            this.ticketingControl1 = new CatalogueManager.LocationsMenu.Ticketing.TicketingControl();
            this.cbDisabled = new System.Windows.Forms.CheckBox();
            this.pPipeline = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.ddExecutionTimescale = new System.Windows.Forms.ComboBox();
            this.saverButton = new CatalogueManager.SimpleControls.ObjectSaverButton();
            this.ragSmiley1 = new ReusableUIComponents.RAGSmiley();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(14, 4);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(207, 13);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Name: I\'ve got a lovely bunch of coconuts";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Comment:";
            // 
            // tbComment
            // 
            this.tbComment.Location = new System.Drawing.Point(74, 34);
            this.tbComment.Name = "tbComment";
            this.tbComment.Size = new System.Drawing.Size(497, 20);
            this.tbComment.TabIndex = 2;
            this.tbComment.TextChanged += new System.EventHandler(this.tbComment_TextChanged);
            // 
            // ticketingControl1
            // 
            this.ticketingControl1.AutoSize = true;
            this.ticketingControl1.Location = new System.Drawing.Point(74, 60);
            this.ticketingControl1.Name = "ticketingControl1";
            this.ticketingControl1.Size = new System.Drawing.Size(300, 52);
            this.ticketingControl1.TabIndex = 1;
            this.ticketingControl1.TicketText = "";
            // 
            // cbDisabled
            // 
            this.cbDisabled.AutoSize = true;
            this.cbDisabled.Location = new System.Drawing.Point(74, 118);
            this.cbDisabled.Name = "cbDisabled";
            this.cbDisabled.Size = new System.Drawing.Size(67, 17);
            this.cbDisabled.TabIndex = 3;
            this.cbDisabled.Text = "Disabled";
            this.cbDisabled.UseVisualStyleBackColor = true;
            this.cbDisabled.CheckedChanged += new System.EventHandler(this.cbDisabled_CheckedChanged);
            // 
            // pPipeline
            // 
            this.pPipeline.Location = new System.Drawing.Point(74, 152);
            this.pPipeline.Name = "pPipeline";
            this.pPipeline.Size = new System.Drawing.Size(746, 146);
            this.pPipeline.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 307);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Execution Timescale:";
            // 
            // ddExecutionTimescale
            // 
            this.ddExecutionTimescale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddExecutionTimescale.FormattingEnabled = true;
            this.ddExecutionTimescale.Location = new System.Drawing.Point(117, 304);
            this.ddExecutionTimescale.Name = "ddExecutionTimescale";
            this.ddExecutionTimescale.Size = new System.Drawing.Size(465, 21);
            this.ddExecutionTimescale.TabIndex = 6;
            this.ddExecutionTimescale.SelectedIndexChanged += new System.EventHandler(this.ddExecutionTimescale_SelectedIndexChanged);
            // 
            // saverButton
            // 
            this.saverButton.Location = new System.Drawing.Point(74, 343);
            this.saverButton.Name = "saverButton";
            this.saverButton.Size = new System.Drawing.Size(76, 23);
            this.saverButton.TabIndex = 7;
            this.saverButton.Text = "Save";
            // 
            // ragSmiley1
            // 
            this.ragSmiley1.AlwaysShowHandCursor = false;
            this.ragSmiley1.BackColor = System.Drawing.Color.Transparent;
            this.ragSmiley1.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.ragSmiley1.Location = new System.Drawing.Point(380, 75);
            this.ragSmiley1.Name = "ragSmiley1";
            this.ragSmiley1.Size = new System.Drawing.Size(25, 25);
            this.ragSmiley1.TabIndex = 8;
            // 
            // AutomateExtractionScheduleTab
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ragSmiley1);
            this.Controls.Add(this.saverButton);
            this.Controls.Add(this.ddExecutionTimescale);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pPipeline);
            this.Controls.Add(this.cbDisabled);
            this.Controls.Add(this.ticketingControl1);
            this.Controls.Add(this.tbComment);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblName);
            this.Name = "AutomateExtractionScheduleTab";
            this.Size = new System.Drawing.Size(835, 564);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbComment;
        private TicketingControl ticketingControl1;
        private System.Windows.Forms.CheckBox cbDisabled;
        private System.Windows.Forms.Panel pPipeline;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox ddExecutionTimescale;
        private ObjectSaverButton saverButton;
        private ReusableUIComponents.RAGSmiley ragSmiley1;
    }
}
