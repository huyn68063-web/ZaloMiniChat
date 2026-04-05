using System;

namespace ZaloMini.Client
{
    partial class FormChat
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lstUsers = new System.Windows.Forms.ListBox();
            this.rtbMessages = new System.Windows.Forms.RichTextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSettings = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tableBottom = new System.Windows.Forms.TableLayoutPanel();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.btnSendFile = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableBottom.SuspendLayout();
            this.SuspendLayout();

            // splitContainer1
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Panel1.Controls.Add(this.lstUsers);
            this.splitContainer1.Panel2.Controls.Add(this.rtbMessages);
            this.splitContainer1.Panel2.Controls.Add(this.progressBar);
            this.splitContainer1.Panel2.Controls.Add(this.tableBottom);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Size = new System.Drawing.Size(800, 500);
            this.splitContainer1.SplitterDistance = 200;
            this.splitContainer1.TabIndex = 0;

            // lstUsers
            this.lstUsers.BackColor = System.Drawing.Color.White;
            this.lstUsers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstUsers.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lstUsers.FormattingEnabled = true;
            this.lstUsers.ItemHeight = 20;
            this.lstUsers.Name = "lstUsers";
            this.lstUsers.TabIndex = 0;

            // panel1 - header xanh
            this.panel1.BackColor = System.Drawing.Color.FromArgb(0, 104, 255);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnSettings);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(600, 45);
            this.panel1.TabIndex = 5;

            // btnSettings
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(0, 104, 255);
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(45, 45);
            this.btnSettings.TabIndex = 0;
            this.btnSettings.Text = "⚙";
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 14F);
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

            // label1
            this.label1.AutoSize = false;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Name = "label1";
            this.label1.Text = "ZaloMini Chat";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // tableBottom - hàng dưới cùng, căn đều 3 control
            this.tableBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableBottom.Height = 40;
            this.tableBottom.ColumnCount = 3;
            this.tableBottom.RowCount = 1;
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 100F));      // txtMessage
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Absolute, 100F));      // btnSendFile
            this.tableBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Absolute, 90F));      // btnSend
            this.tableBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 100F));
            this.tableBottom.Controls.Add(this.txtMessage, 0, 0);
            this.tableBottom.Controls.Add(this.btnSendFile, 1, 0);
            this.tableBottom.Controls.Add(this.btnSend, 2, 0);
            this.tableBottom.Padding = new System.Windows.Forms.Padding(4, 4, 8, 4);
            this.tableBottom.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tableBottom.Name = "tableBottom";
            this.tableBottom.TabIndex = 6;

            // txtMessage
            this.txtMessage.BackColor = System.Drawing.Color.White;
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.txtMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.TabIndex = 1;
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMessage_KeyDown);

            // btnSendFile
            this.btnSendFile.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.btnSendFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendFile.FlatAppearance.BorderSize = 0;
            this.btnSendFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSendFile.Name = "btnSendFile";
            this.btnSendFile.TabIndex = 2;
            this.btnSendFile.Text = "📎 File";   
            this.btnSendFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.btnSendFile.UseVisualStyleBackColor = false;
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);

            // btnSend
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(0, 104, 255);
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSend.Name = "btnSend";
            this.btnSend.TabIndex = 3;
            this.btnSend.Text = "Gửi";
            this.btnSend.UseVisualStyleBackColor = false;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);

            // rtbMessages
            this.rtbMessages.BackColor = System.Drawing.Color.White;
            this.rtbMessages.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbMessages.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.rtbMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbMessages.Name = "rtbMessages";
            this.rtbMessages.ReadOnly = true;
            this.rtbMessages.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbMessages.TabIndex = 0;
            this.rtbMessages.Margin = new System.Windows.Forms.Padding(8);

            // progressBar
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar.Height = 6;
            this.progressBar.Name = "progressBar";
            this.progressBar.TabIndex = 4;
            this.progressBar.Visible = false;

            // FormChat
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.splitContainer1);
            this.MinimumSize = new System.Drawing.Size(700, 450);
            this.Name = "FormChat";
            this.Text = "ZaloMini";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormChat_FormClosing);
            this.Load += new System.EventHandler(this.FormChat_Load);

            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tableBottom.ResumeLayout(false);
            this.tableBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox lstUsers;
        private System.Windows.Forms.RichTextBox rtbMessages;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSendFile;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.TableLayoutPanel tableBottom;
    }
}