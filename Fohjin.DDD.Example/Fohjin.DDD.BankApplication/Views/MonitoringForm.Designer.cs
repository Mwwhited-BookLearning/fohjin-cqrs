namespace Fohjin.DDD.BankApplication.Views;

partial class MonitoringForm
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
        this._splitContainer = new System.Windows.Forms.SplitContainer();
        this._logsGroupBox = new System.Windows.Forms.GroupBox();
        this._logsListBox = new System.Windows.Forms.ListBox();
        this._eventsGroupBox = new System.Windows.Forms.GroupBox();
        this._eventsListBox = new System.Windows.Forms.ListBox();
        ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
        this._splitContainer.Panel1.SuspendLayout();
        this._splitContainer.Panel2.SuspendLayout();
        this._splitContainer.SuspendLayout();
        this._logsGroupBox.SuspendLayout();
        this._eventsGroupBox.SuspendLayout();
        this.SuspendLayout();
        //
        // _splitContainer
        //
        this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        this._splitContainer.Location = new System.Drawing.Point(0, 0);
        this._splitContainer.Name = "_splitContainer";
        //
        // _splitContainer.Panel1
        //
        this._splitContainer.Panel1.Controls.Add(this._logsGroupBox);
        //
        // _splitContainer.Panel2
        //
        this._splitContainer.Panel2.Controls.Add(this._eventsGroupBox);
        this._splitContainer.Size = new System.Drawing.Size(800, 450);
        this._splitContainer.SplitterDistance = 400;
        this._splitContainer.TabIndex = 0;
        //
        // _logsGroupBox
        //
        this._logsGroupBox.Controls.Add(this._logsListBox);
        this._logsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._logsGroupBox.Location = new System.Drawing.Point(0, 0);
        this._logsGroupBox.Name = "_logsGroupBox";
        this._logsGroupBox.Size = new System.Drawing.Size(400, 450);
        this._logsGroupBox.TabIndex = 0;
        this._logsGroupBox.TabStop = false;
        this._logsGroupBox.Text = "Logs";
        //
        // _logsListBox
        //
        this._logsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._logsListBox.HorizontalScrollbar = true;
        this._logsListBox.IntegralHeight = false;
        this._logsListBox.Location = new System.Drawing.Point(3, 19);
        this._logsListBox.Name = "_logsListBox";
        this._logsListBox.Size = new System.Drawing.Size(394, 428);
        this._logsListBox.TabIndex = 0;
        //
        // _eventsGroupBox
        //
        this._eventsGroupBox.Controls.Add(this._eventsListBox);
        this._eventsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._eventsGroupBox.Location = new System.Drawing.Point(0, 0);
        this._eventsGroupBox.Name = "_eventsGroupBox";
        this._eventsGroupBox.Size = new System.Drawing.Size(396, 450);
        this._eventsGroupBox.TabIndex = 0;
        this._eventsGroupBox.TabStop = false;
        this._eventsGroupBox.Text = "Events";
        //
        // _eventsListBox
        //
        this._eventsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this._eventsListBox.HorizontalScrollbar = true;
        this._eventsListBox.IntegralHeight = false;
        this._eventsListBox.Location = new System.Drawing.Point(3, 19);
        this._eventsListBox.Name = "_eventsListBox";
        this._eventsListBox.Size = new System.Drawing.Size(390, 428);
        this._eventsListBox.TabIndex = 0;
        //
        // MonitoringForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this._splitContainer);
        this.Name = "MonitoringForm";
        this.Text = "Monitoring";
        this._splitContainer.Panel1.ResumeLayout(false);
        this._splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
        this._splitContainer.ResumeLayout(false);
        this._logsGroupBox.ResumeLayout(false);
        this._eventsGroupBox.ResumeLayout(false);
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.SplitContainer _splitContainer;
    private System.Windows.Forms.GroupBox _logsGroupBox;
    private System.Windows.Forms.ListBox _logsListBox;
    private System.Windows.Forms.GroupBox _eventsGroupBox;
    private System.Windows.Forms.ListBox _eventsListBox;
}
