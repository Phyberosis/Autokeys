namespace AutoKeys
{
    partial class wndMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(wndMain));
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.pnlSearch = new System.Windows.Forms.Panel();
            this.lblZoom = new System.Windows.Forms.Label();
            this.txtZoom = new System.Windows.Forms.TextBox();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.txtSpeed = new System.Windows.Forms.TextBox();
            this.chkInstant = new System.Windows.Forms.CheckBox();
            this.lblAltT = new System.Windows.Forms.Label();
            this.txtAltTab = new System.Windows.Forms.TextBox();
            this.txtDelay = new System.Windows.Forms.TextBox();
            this.lblDelay = new System.Windows.Forms.Label();
            this.txtFont = new System.Windows.Forms.TextBox();
            this.lblFont = new System.Windows.Forms.Label();
            this.lblSearch = new System.Windows.Forms.Label();
            this.dataView = new System.Windows.Forms.DataGridView();
            this.tag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.msg = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.wrkTyper = new System.ComponentModel.BackgroundWorker();
            this.tmrAsync = new System.Windows.Forms.Timer(this.components);
            this.pnlSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataView)).BeginInit();
            this.SuspendLayout();
            // 
            // txtSearch
            // 
            this.txtSearch.AcceptsTab = true;
            this.txtSearch.Location = new System.Drawing.Point(133, 6);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(231, 20);
            this.txtSearch.TabIndex = 0;
            // 
            // pnlSearch
            // 
            this.pnlSearch.AutoSize = true;
            this.pnlSearch.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlSearch.Controls.Add(this.lblZoom);
            this.pnlSearch.Controls.Add(this.txtZoom);
            this.pnlSearch.Controls.Add(this.lblSpeed);
            this.pnlSearch.Controls.Add(this.txtSpeed);
            this.pnlSearch.Controls.Add(this.chkInstant);
            this.pnlSearch.Controls.Add(this.lblAltT);
            this.pnlSearch.Controls.Add(this.txtAltTab);
            this.pnlSearch.Controls.Add(this.txtDelay);
            this.pnlSearch.Controls.Add(this.lblDelay);
            this.pnlSearch.Controls.Add(this.txtFont);
            this.pnlSearch.Controls.Add(this.lblFont);
            this.pnlSearch.Controls.Add(this.lblSearch);
            this.pnlSearch.Controls.Add(this.txtSearch);
            this.pnlSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSearch.Location = new System.Drawing.Point(0, 0);
            this.pnlSearch.Name = "pnlSearch";
            this.pnlSearch.Size = new System.Drawing.Size(1105, 31);
            this.pnlSearch.TabIndex = 1;
            this.pnlSearch.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlSearch_Paint);
            // 
            // lblZoom
            // 
            this.lblZoom.AutoSize = true;
            this.lblZoom.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblZoom.Location = new System.Drawing.Point(403, 5);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(50, 20);
            this.lblZoom.TabIndex = 12;
            this.lblZoom.Text = "Zoom";
            // 
            // txtZoom
            // 
            this.txtZoom.Location = new System.Drawing.Point(461, 6);
            this.txtZoom.Name = "txtZoom";
            this.txtZoom.Size = new System.Drawing.Size(50, 20);
            this.txtZoom.TabIndex = 11;
            this.txtZoom.Leave += new System.EventHandler(this.txtZoom_Leave);
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSpeed.Location = new System.Drawing.Point(516, 5);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(56, 20);
            this.lblSpeed.TabIndex = 10;
            this.lblSpeed.Text = "Speed";
            // 
            // txtSpeed
            // 
            this.txtSpeed.Location = new System.Drawing.Point(578, 5);
            this.txtSpeed.Name = "txtSpeed";
            this.txtSpeed.Size = new System.Drawing.Size(50, 20);
            this.txtSpeed.TabIndex = 9;
            this.txtSpeed.Leave += new System.EventHandler(this.txtSpeed_Leave);
            // 
            // chkInstant
            // 
            this.chkInstant.AutoSize = true;
            this.chkInstant.Location = new System.Drawing.Point(3, 7);
            this.chkInstant.Name = "chkInstant";
            this.chkInstant.Size = new System.Drawing.Size(58, 17);
            this.chkInstant.TabIndex = 8;
            this.chkInstant.Text = "Instant";
            this.chkInstant.UseVisualStyleBackColor = true;
            this.chkInstant.CheckedChanged += new System.EventHandler(this.chkInstant_CheckedChanged);
            // 
            // lblAltT
            // 
            this.lblAltT.AutoSize = true;
            this.lblAltT.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAltT.Location = new System.Drawing.Point(634, 6);
            this.lblAltT.Name = "lblAltT";
            this.lblAltT.Size = new System.Drawing.Size(80, 20);
            this.lblAltT.TabIndex = 7;
            this.lblAltT.Text = "Tab Delay";
            // 
            // txtAltTab
            // 
            this.txtAltTab.Location = new System.Drawing.Point(720, 6);
            this.txtAltTab.Name = "txtAltTab";
            this.txtAltTab.Size = new System.Drawing.Size(50, 20);
            this.txtAltTab.TabIndex = 6;
            this.txtAltTab.Leave += new System.EventHandler(this.txtAltTab_Leave);
            // 
            // txtDelay
            // 
            this.txtDelay.Location = new System.Drawing.Point(1045, 5);
            this.txtDelay.Name = "txtDelay";
            this.txtDelay.Size = new System.Drawing.Size(50, 20);
            this.txtDelay.TabIndex = 5;
            this.txtDelay.Leave += new System.EventHandler(this.txtDelay_Leave);
            // 
            // lblDelay
            // 
            this.lblDelay.AutoSize = true;
            this.lblDelay.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDelay.Location = new System.Drawing.Point(931, 5);
            this.lblDelay.Name = "lblDelay";
            this.lblDelay.Size = new System.Drawing.Size(108, 20);
            this.lblDelay.TabIndex = 4;
            this.lblDelay.Text = "Confirm Delay";
            // 
            // txtFont
            // 
            this.txtFont.Location = new System.Drawing.Point(875, 8);
            this.txtFont.Name = "txtFont";
            this.txtFont.Size = new System.Drawing.Size(50, 20);
            this.txtFont.TabIndex = 3;
            this.txtFont.Leave += new System.EventHandler(this.txtFont_Leave);
            // 
            // lblFont
            // 
            this.lblFont.AutoSize = true;
            this.lblFont.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFont.Location = new System.Drawing.Point(776, 5);
            this.lblFont.Name = "lblFont";
            this.lblFont.Size = new System.Drawing.Size(77, 20);
            this.lblFont.TabIndex = 2;
            this.lblFont.Text = "Font Size";
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSearch.Location = new System.Drawing.Point(67, 5);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(60, 20);
            this.lblSearch.TabIndex = 1;
            this.lblSearch.Text = "Search";
            // 
            // dataView
            // 
            this.dataView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tag,
            this.grp,
            this.col,
            this.msg});
            this.dataView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataView.Location = new System.Drawing.Point(0, 31);
            this.dataView.Name = "dataView";
            this.dataView.Size = new System.Drawing.Size(1105, 623);
            this.dataView.TabIndex = 3;
            this.dataView.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dataView_CellBeginEdit);
            this.dataView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataView_CellEndEdit);
            this.dataView.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataView_CellMouseClick);
            this.dataView.Sorted += new System.EventHandler(this.dataView_Sorted);
            this.dataView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataView_KeyDown);
            this.dataView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.dataView_MouseClick);
            // 
            // tag
            // 
            this.tag.HeaderText = "Macro Tag";
            this.tag.MinimumWidth = 85;
            this.tag.Name = "tag";
            // 
            // grp
            // 
            this.grp.HeaderText = "Group";
            this.grp.MinimumWidth = 40;
            this.grp.Name = "grp";
            // 
            // col
            // 
            this.col.HeaderText = "Color";
            this.col.MinimumWidth = 40;
            this.col.Name = "col";
            // 
            // msg
            // 
            this.msg.HeaderText = "Message";
            this.msg.MinimumWidth = 100;
            this.msg.Name = "msg";
            // 
            // wrkTyper
            // 
            this.wrkTyper.DoWork += new System.ComponentModel.DoWorkEventHandler(this.wrkTyper_DoWork);
            // 
            // tmrAsync
            // 
            this.tmrAsync.Interval = 500;
            this.tmrAsync.Tick += new System.EventHandler(this.tmrAsyncTick);
            // 
            // wndMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1105, 654);
            this.Controls.Add(this.dataView);
            this.Controls.Add(this.pnlSearch);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(750, 398);
            this.Name = "wndMain";
            this.Text = "Autokeys";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.wndMain_FormClosing);
            this.Load += new System.EventHandler(this.ini);
            this.ResizeEnd += new System.EventHandler(this.wndMain_ResizeEnd);
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Panel pnlSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.DataGridView dataView;
        private System.ComponentModel.BackgroundWorker wrkTyper;
        private System.Windows.Forms.TextBox txtDelay;
        private System.Windows.Forms.Label lblDelay;
        private System.Windows.Forms.TextBox txtFont;
        private System.Windows.Forms.Label lblFont;
        private System.Windows.Forms.Label lblAltT;
        private System.Windows.Forms.TextBox txtAltTab;
        private System.Windows.Forms.CheckBox chkInstant;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.TextBox txtSpeed;
        private System.Windows.Forms.DataGridViewTextBoxColumn tag;
        private System.Windows.Forms.DataGridViewTextBoxColumn grp;
        private System.Windows.Forms.DataGridViewTextBoxColumn col;
        private System.Windows.Forms.DataGridViewTextBoxColumn msg;
        private System.Windows.Forms.Timer tmrAsync;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.TextBox txtZoom;
    }
}

