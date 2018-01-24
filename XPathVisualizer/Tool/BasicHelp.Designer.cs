namespace XPathVisualizer
{
    partial class BasicHelp
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
            this.ImagePictureBox = new System.Windows.Forms.PictureBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.rtbHelp = new System.Windows.Forms.RichTextBox();
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.AppTitleLabel = new System.Windows.Forms.Label();
            this.AppDescriptionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // ImagePictureBox
            //
            this.ImagePictureBox.Location = new System.Drawing.Point(14, 7);
            this.ImagePictureBox.Name = "ImagePictureBox";
            this.ImagePictureBox.Size = new System.Drawing.Size(32, 32);
            this.ImagePictureBox.TabIndex = 24;
            this.ImagePictureBox.TabStop = false;
            //
            // AppTitleLabel
            //
            this.AppTitleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                                                              | System.Windows.Forms.AnchorStyles.Right)));
            this.AppTitleLabel.Location = new System.Drawing.Point(58, 7);
            this.AppTitleLabel.Name = "AppTitleLabel";
            this.AppTitleLabel.Size = new System.Drawing.Size(328, 16);
            this.AppTitleLabel.TabIndex = 17;
            this.AppTitleLabel.Text = "%title%";
            //
            // AppDescriptionLabel
            //
            this.AppDescriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                                                                    | System.Windows.Forms.AnchorStyles.Right)));
            this.AppDescriptionLabel.Location = new System.Drawing.Point(58, 27);
            this.AppDescriptionLabel.Name = "AppDescriptionLabel";
            this.AppDescriptionLabel.Size = new System.Drawing.Size(328, 16);
            this.AppDescriptionLabel.TabIndex = 19;
            this.AppDescriptionLabel.Text = "%description%";
            //
            // GroupBox1
            //
            this.GroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                                                          | System.Windows.Forms.AnchorStyles.Right)));
            this.GroupBox1.Location = new System.Drawing.Point(6, 47);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.Size = new System.Drawing.Size(380, 2);
            this.GroupBox1.TabIndex = 18;
            this.GroupBox1.TabStop = false;
            this.GroupBox1.Text = "GroupBox1";
            //
            // rtbHelp
            //
            this.rtbHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                                 | System.Windows.Forms.AnchorStyles.Left)
                                                                                | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbHelp.BackColor = System.Drawing.SystemColors.ControlLight;
            this.rtbHelp.Location = new System.Drawing.Point(6, 53);
            this.rtbHelp.Name = "rtbHelp";
            this.rtbHelp.ReadOnly = true;
            this.rtbHelp.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical |
                System.Windows.Forms.RichTextBoxScrollBars.Horizontal;
            this.rtbHelp.Size = new System.Drawing.Size(380, 184);
            this.rtbHelp.TabIndex = 26;
            this.rtbHelp.Text = "%product% is %copyright%, %trademark%";
            this.rtbHelp.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.rtbHelp_LinkClicked);
            //
            // OKButton
            //
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.OKButton.Location = new System.Drawing.Point(312, 245);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(76, 23);
            this.OKButton.TabIndex = 16;
            this.OKButton.Text = "OK";
            //
            // BasicHelp
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.OKButton;
            this.ClientSize = new System.Drawing.Size(394, 275);
            this.Controls.Add(this.ImagePictureBox);
            this.Controls.Add(this.AppDescriptionLabel);
            this.Controls.Add(this.AppTitleLabel);
            this.Controls.Add(this.GroupBox1);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.rtbHelp);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BasicHelp";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "%title% Help";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.BasicHelp_Paint);
            this.Load += new System.EventHandler(this.BasicHelp_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ImagePictureBox)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox ImagePictureBox;
        private System.Windows.Forms.Button OKButton;
        internal System.Windows.Forms.RichTextBox rtbHelp;
        private System.Windows.Forms.Label AppDescriptionLabel;
        private System.Windows.Forms.Label AppTitleLabel;
        private System.Windows.Forms.GroupBox GroupBox1;
    }
}