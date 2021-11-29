﻿namespace com.mirle.ibg3k0.bc.winform.UI
{
    partial class HistoryAlarmsForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.skinGroupBox2 = new CCWin.SkinControl.SkinGroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.m_EqptIDCbx = new System.Windows.Forms.ComboBox();
            this.skinGroupBox4 = new CCWin.SkinControl.SkinGroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.m_EndDTCbx = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.m_StartDTCbx = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.skinGroupBox1 = new CCWin.SkinControl.SkinGroupBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.m_AlarmCodeTbl = new System.Windows.Forms.MaskedTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.m_exportBtn = new CCWin.SkinControl.SkinButton();
            this.btnlSearch = new CCWin.SkinControl.SkinButton();
            this.skinGroupBox3 = new CCWin.SkinControl.SkinGroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.txt_cstID = new System.Windows.Forms.TextBox();
            this.dgv_alarms = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.CommandID1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CommandID2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CommandID3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CommandID4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aLAMCODEDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.eQPTIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aLAMSTATDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aLAMLVLDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rPTDATETIMEDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cLEARDATETIMEDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aLAMDESCDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aLARMObjToShowBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel1.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.skinGroupBox2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.skinGroupBox4.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.skinGroupBox1.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.panel2.SuspendLayout();
            this.skinGroupBox3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_alarms)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.aLARMObjToShowBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanel6);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1596, 110);
            this.panel1.TabIndex = 11;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 5;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 400F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.Controls.Add(this.skinGroupBox2, 2, 0);
            this.tableLayoutPanel6.Controls.Add(this.skinGroupBox4, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.skinGroupBox1, 1, 0);
            this.tableLayoutPanel6.Controls.Add(this.panel2, 4, 0);
            this.tableLayoutPanel6.Controls.Add(this.skinGroupBox3, 3, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(1596, 110);
            this.tableLayoutPanel6.TabIndex = 85;
            // 
            // skinGroupBox2
            // 
            this.skinGroupBox2.BackColor = System.Drawing.Color.Transparent;
            this.skinGroupBox2.BorderColor = System.Drawing.Color.Black;
            this.skinGroupBox2.Controls.Add(this.tableLayoutPanel4);
            this.skinGroupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skinGroupBox2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.skinGroupBox2.Font = new System.Drawing.Font("Arial", 15F);
            this.skinGroupBox2.ForeColor = System.Drawing.Color.Black;
            this.skinGroupBox2.Location = new System.Drawing.Point(653, 3);
            this.skinGroupBox2.Name = "skinGroupBox2";
            this.skinGroupBox2.RectBackColor = System.Drawing.SystemColors.Control;
            this.skinGroupBox2.RoundStyle = CCWin.SkinClass.RoundStyle.All;
            this.skinGroupBox2.Size = new System.Drawing.Size(244, 104);
            this.skinGroupBox2.TabIndex = 75;
            this.skinGroupBox2.TabStop = false;
            this.skinGroupBox2.Text = "Equipment ID ";
            this.skinGroupBox2.TitleBorderColor = System.Drawing.Color.Black;
            this.skinGroupBox2.TitleRectBackColor = System.Drawing.Color.LightSteelBlue;
            this.skinGroupBox2.TitleRoundStyle = CCWin.SkinClass.RoundStyle.All;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Controls.Add(this.m_EqptIDCbx, 0, 0);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 26);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(238, 75);
            this.tableLayoutPanel4.TabIndex = 84;
            // 
            // m_EqptIDCbx
            // 
            this.m_EqptIDCbx.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.m_EqptIDCbx.Font = new System.Drawing.Font("Arial", 14F);
            this.m_EqptIDCbx.FormattingEnabled = true;
            this.m_EqptIDCbx.Location = new System.Drawing.Point(5, 22);
            this.m_EqptIDCbx.Name = "m_EqptIDCbx";
            this.m_EqptIDCbx.Size = new System.Drawing.Size(227, 30);
            this.m_EqptIDCbx.TabIndex = 53;
            this.m_EqptIDCbx.SelectedValueChanged += new System.EventHandler(this.m_EqptIDCbx_SelectedValueChanged);
            // 
            // skinGroupBox4
            // 
            this.skinGroupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.skinGroupBox4.BackColor = System.Drawing.Color.Transparent;
            this.skinGroupBox4.BorderColor = System.Drawing.Color.Black;
            this.skinGroupBox4.Controls.Add(this.tableLayoutPanel3);
            this.skinGroupBox4.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.skinGroupBox4.Font = new System.Drawing.Font("Arial", 14F);
            this.skinGroupBox4.ForeColor = System.Drawing.Color.Black;
            this.skinGroupBox4.Location = new System.Drawing.Point(3, 3);
            this.skinGroupBox4.Name = "skinGroupBox4";
            this.skinGroupBox4.RectBackColor = System.Drawing.SystemColors.Control;
            this.skinGroupBox4.RoundStyle = CCWin.SkinClass.RoundStyle.All;
            this.skinGroupBox4.Size = new System.Drawing.Size(393, 104);
            this.skinGroupBox4.TabIndex = 77;
            this.skinGroupBox4.TabStop = false;
            this.skinGroupBox4.Text = "    Alarm Set Time ";
            this.skinGroupBox4.TitleBorderColor = System.Drawing.Color.Black;
            this.skinGroupBox4.TitleRectBackColor = System.Drawing.Color.LightSteelBlue;
            this.skinGroupBox4.TitleRoundStyle = CCWin.SkinClass.RoundStyle.All;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36.12903F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 63.87097F));
            this.tableLayoutPanel3.Controls.Add(this.m_EndDTCbx, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.m_StartDTCbx, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 25);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(387, 76);
            this.tableLayoutPanel3.TabIndex = 79;
            // 
            // m_EndDTCbx
            // 
            this.m_EndDTCbx.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.m_EndDTCbx.CustomFormat = "yyyy/MM/dd HH:mm:ss";
            this.m_EndDTCbx.Font = new System.Drawing.Font("Arial", 14F);
            this.m_EndDTCbx.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.m_EndDTCbx.Location = new System.Drawing.Point(142, 42);
            this.m_EndDTCbx.Name = "m_EndDTCbx";
            this.m_EndDTCbx.Size = new System.Drawing.Size(242, 29);
            this.m_EndDTCbx.TabIndex = 66;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Arial", 14F);
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(3, 8);
            this.label3.Margin = new System.Windows.Forms.Padding(3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 22);
            this.label3.TabIndex = 57;
            this.label3.Text = "From Time";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // m_StartDTCbx
            // 
            this.m_StartDTCbx.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.m_StartDTCbx.CustomFormat = "yyyy/MM/dd HH:mm:ss";
            this.m_StartDTCbx.Font = new System.Drawing.Font("Arial", 14F);
            this.m_StartDTCbx.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.m_StartDTCbx.Location = new System.Drawing.Point(142, 4);
            this.m_StartDTCbx.Name = "m_StartDTCbx";
            this.m_StartDTCbx.Size = new System.Drawing.Size(242, 29);
            this.m_StartDTCbx.TabIndex = 65;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 14F);
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(3, 46);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 22);
            this.label1.TabIndex = 61;
            this.label1.Text = "End Time ";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // skinGroupBox1
            // 
            this.skinGroupBox1.BackColor = System.Drawing.Color.Transparent;
            this.skinGroupBox1.BorderColor = System.Drawing.Color.Black;
            this.skinGroupBox1.Controls.Add(this.tableLayoutPanel5);
            this.skinGroupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skinGroupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.skinGroupBox1.Font = new System.Drawing.Font("Arial", 14F);
            this.skinGroupBox1.ForeColor = System.Drawing.Color.Black;
            this.skinGroupBox1.Location = new System.Drawing.Point(403, 3);
            this.skinGroupBox1.Name = "skinGroupBox1";
            this.skinGroupBox1.RectBackColor = System.Drawing.SystemColors.Control;
            this.skinGroupBox1.RoundStyle = CCWin.SkinClass.RoundStyle.All;
            this.skinGroupBox1.Size = new System.Drawing.Size(244, 104);
            this.skinGroupBox1.TabIndex = 76;
            this.skinGroupBox1.TabStop = false;
            this.skinGroupBox1.Text = "Alarm Code ";
            this.skinGroupBox1.TitleBorderColor = System.Drawing.Color.Black;
            this.skinGroupBox1.TitleRectBackColor = System.Drawing.Color.LightSteelBlue;
            this.skinGroupBox1.TitleRoundStyle = CCWin.SkinClass.RoundStyle.All;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel5.Controls.Add(this.m_AlarmCodeTbl, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 25);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(238, 76);
            this.tableLayoutPanel5.TabIndex = 84;
            // 
            // m_AlarmCodeTbl
            // 
            this.m_AlarmCodeTbl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.m_AlarmCodeTbl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_AlarmCodeTbl.Font = new System.Drawing.Font("Arial", 14F);
            this.m_AlarmCodeTbl.Location = new System.Drawing.Point(5, 23);
            this.m_AlarmCodeTbl.Name = "m_AlarmCodeTbl";
            this.m_AlarmCodeTbl.PromptChar = ' ';
            this.m_AlarmCodeTbl.Size = new System.Drawing.Size(227, 29);
            this.m_AlarmCodeTbl.TabIndex = 64;
            this.m_AlarmCodeTbl.TextChanged += new System.EventHandler(this.m_AlarmCodeTbl_TextChanged);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.m_exportBtn);
            this.panel2.Controls.Add(this.btnlSearch);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(1153, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(440, 104);
            this.panel2.TabIndex = 78;
            // 
            // m_exportBtn
            // 
            this.m_exportBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.m_exportBtn.BackColor = System.Drawing.Color.Transparent;
            this.m_exportBtn.BaseColor = System.Drawing.Color.LightGray;
            this.m_exportBtn.BorderColor = System.Drawing.Color.Black;
            this.m_exportBtn.ControlState = CCWin.SkinClass.ControlState.Normal;
            this.m_exportBtn.DownBack = null;
            this.m_exportBtn.DownBaseColor = System.Drawing.Color.RoyalBlue;
            this.m_exportBtn.Font = new System.Drawing.Font("Arial", 14.25F);
            this.m_exportBtn.Image = global::com.mirle.ibg3k0.bc.winform.Properties.Resources.export;
            this.m_exportBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.m_exportBtn.ImageSize = new System.Drawing.Size(24, 24);
            this.m_exportBtn.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.m_exportBtn.Location = new System.Drawing.Point(307, 66);
            this.m_exportBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.m_exportBtn.MouseBack = null;
            this.m_exportBtn.Name = "m_exportBtn";
            this.m_exportBtn.NormlBack = null;
            this.m_exportBtn.RoundStyle = CCWin.SkinClass.RoundStyle.All;
            this.m_exportBtn.Size = new System.Drawing.Size(127, 33);
            this.m_exportBtn.TabIndex = 85;
            this.m_exportBtn.Text = "   Export";
            this.m_exportBtn.UseVisualStyleBackColor = false;
            this.m_exportBtn.Click += new System.EventHandler(this.m_exportBtn_Click);
            // 
            // btnlSearch
            // 
            this.btnlSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnlSearch.BackColor = System.Drawing.Color.Transparent;
            this.btnlSearch.BaseColor = System.Drawing.Color.LightGray;
            this.btnlSearch.BorderColor = System.Drawing.Color.Black;
            this.btnlSearch.ControlState = CCWin.SkinClass.ControlState.Normal;
            this.btnlSearch.DownBack = null;
            this.btnlSearch.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnlSearch.Font = new System.Drawing.Font("Arial", 14F);
            this.btnlSearch.ForeColor = System.Drawing.Color.Black;
            this.btnlSearch.Image = global::com.mirle.ibg3k0.bc.winform.Properties.Resources.se;
            this.btnlSearch.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnlSearch.ImageSize = new System.Drawing.Size(24, 24);
            this.btnlSearch.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnlSearch.Location = new System.Drawing.Point(307, 25);
            this.btnlSearch.MouseBack = null;
            this.btnlSearch.Name = "btnlSearch";
            this.btnlSearch.NormlBack = null;
            this.btnlSearch.RoundStyle = CCWin.SkinClass.RoundStyle.All;
            this.btnlSearch.Size = new System.Drawing.Size(127, 32);
            this.btnlSearch.TabIndex = 83;
            this.btnlSearch.Text = "Search";
            this.btnlSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnlSearch.UseVisualStyleBackColor = false;
            this.btnlSearch.Click += new System.EventHandler(this.btnlSearch_Click);
            // 
            // skinGroupBox3
            // 
            this.skinGroupBox3.BackColor = System.Drawing.Color.Transparent;
            this.skinGroupBox3.BorderColor = System.Drawing.Color.Black;
            this.skinGroupBox3.Controls.Add(this.tableLayoutPanel2);
            this.skinGroupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skinGroupBox3.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.skinGroupBox3.Font = new System.Drawing.Font("Arial", 15F);
            this.skinGroupBox3.ForeColor = System.Drawing.Color.Black;
            this.skinGroupBox3.Location = new System.Drawing.Point(903, 3);
            this.skinGroupBox3.Name = "skinGroupBox3";
            this.skinGroupBox3.RectBackColor = System.Drawing.SystemColors.Control;
            this.skinGroupBox3.RoundStyle = CCWin.SkinClass.RoundStyle.All;
            this.skinGroupBox3.Size = new System.Drawing.Size(244, 104);
            this.skinGroupBox3.TabIndex = 75;
            this.skinGroupBox3.TabStop = false;
            this.skinGroupBox3.Text = "CST ID ";
            this.skinGroupBox3.TitleBorderColor = System.Drawing.Color.Black;
            this.skinGroupBox3.TitleRectBackColor = System.Drawing.Color.LightSteelBlue;
            this.skinGroupBox3.TitleRoundStyle = CCWin.SkinClass.RoundStyle.All;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.txt_cstID, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 26);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(238, 75);
            this.tableLayoutPanel2.TabIndex = 84;
            // 
            // txt_cstID
            // 
            this.txt_cstID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_cstID.Location = new System.Drawing.Point(3, 22);
            this.txt_cstID.Name = "txt_cstID";
            this.txt_cstID.Size = new System.Drawing.Size(232, 30);
            this.txt_cstID.TabIndex = 0;
            this.txt_cstID.TextChanged += new System.EventHandler(this.txt_cstID_TextChanged);
            // 
            // dgv_TransferCommand
            // 
            this.dgv_alarms.AllowUserToAddRows = false;
            this.dgv_alarms.AutoGenerateColumns = false;
            this.dgv_alarms.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgv_alarms.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.dgv_alarms.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_alarms.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.aLAMCODEDataGridViewTextBoxColumn,
            this.eQPTIDDataGridViewTextBoxColumn,
            this.aLAMSTATDataGridViewTextBoxColumn,
            this.aLAMLVLDataGridViewTextBoxColumn,
            this.rPTDATETIMEDataGridViewTextBoxColumn,
            this.cLEARDATETIMEDataGridViewTextBoxColumn,
            this.aLAMDESCDataGridViewTextBoxColumn,
            this.CommandID1,
            this.CommandID2,
            this.CommandID3,
            this.CommandID4});
            this.dgv_alarms.DataSource = this.aLARMObjToShowBindingSource;
            this.dgv_alarms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_alarms.GridColor = System.Drawing.SystemColors.ControlDarkDark;
            this.dgv_alarms.Location = new System.Drawing.Point(3, 119);
            this.dgv_alarms.MultiSelect = false;
            this.dgv_alarms.Name = "dgv_TransferCommand";
            this.dgv_alarms.ReadOnly = true;
            this.dgv_alarms.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.dgv_alarms.RowTemplate.Height = 24;
            this.dgv_alarms.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_alarms.Size = new System.Drawing.Size(1596, 594);
            this.dgv_alarms.TabIndex = 9;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.dgv_alarms, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.80447F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75.97765F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1602, 716);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // CommandID1
            // 
            this.CommandID1.DataPropertyName = "CommandID1";
            this.CommandID1.HeaderText = "CommandID 1";
            this.CommandID1.Name = "CommandID1";
            this.CommandID1.ReadOnly = true;
            this.CommandID1.Width = 145;
            // 
            // CommandID2
            // 
            this.CommandID2.DataPropertyName = "CommandID2";
            this.CommandID2.HeaderText = "CommandID 2";
            this.CommandID2.Name = "CommandID2";
            this.CommandID2.ReadOnly = true;
            this.CommandID2.Width = 145;
            // 
            // CommandID3
            // 
            this.CommandID3.DataPropertyName = "CommandID3";
            this.CommandID3.HeaderText = "CommandID 3";
            this.CommandID3.Name = "CommandID3";
            this.CommandID3.ReadOnly = true;
            this.CommandID3.Width = 145;
            // 
            // CommandID4
            // 
            this.CommandID4.DataPropertyName = "CommandID4";
            this.CommandID4.HeaderText = "CommandID 4";
            this.CommandID4.Name = "CommandID4";
            this.CommandID4.ReadOnly = true;
            this.CommandID4.Width = 145;
            // 
            // aLAMCODEDataGridViewTextBoxColumn
            // 
            this.aLAMCODEDataGridViewTextBoxColumn.DataPropertyName = "ALAM_CODE";
            this.aLAMCODEDataGridViewTextBoxColumn.FillWeight = 50F;
            this.aLAMCODEDataGridViewTextBoxColumn.HeaderText = "ID";
            this.aLAMCODEDataGridViewTextBoxColumn.Name = "aLAMCODEDataGridViewTextBoxColumn";
            this.aLAMCODEDataGridViewTextBoxColumn.ReadOnly = true;
            this.aLAMCODEDataGridViewTextBoxColumn.Width = 55;
            // 
            // eQPTIDDataGridViewTextBoxColumn
            // 
            this.eQPTIDDataGridViewTextBoxColumn.DataPropertyName = "EQPT_ID";
            this.eQPTIDDataGridViewTextBoxColumn.FillWeight = 50F;
            this.eQPTIDDataGridViewTextBoxColumn.HeaderText = "Device ID";
            this.eQPTIDDataGridViewTextBoxColumn.Name = "eQPTIDDataGridViewTextBoxColumn";
            this.eQPTIDDataGridViewTextBoxColumn.ReadOnly = true;
            this.eQPTIDDataGridViewTextBoxColumn.Width = 125;
            // 
            // aLAMSTATDataGridViewTextBoxColumn
            // 
            this.aLAMSTATDataGridViewTextBoxColumn.DataPropertyName = "ALAM_STAT";
            this.aLAMSTATDataGridViewTextBoxColumn.FillWeight = 50F;
            this.aLAMSTATDataGridViewTextBoxColumn.HeaderText = "State";
            this.aLAMSTATDataGridViewTextBoxColumn.Name = "aLAMSTATDataGridViewTextBoxColumn";
            this.aLAMSTATDataGridViewTextBoxColumn.ReadOnly = true;
            this.aLAMSTATDataGridViewTextBoxColumn.Width = 85;
            // 
            // aLAMLVLDataGridViewTextBoxColumn
            // 
            this.aLAMLVLDataGridViewTextBoxColumn.DataPropertyName = "ALAM_LVL";
            this.aLAMLVLDataGridViewTextBoxColumn.FillWeight = 50F;
            this.aLAMLVLDataGridViewTextBoxColumn.HeaderText = "Level";
            this.aLAMLVLDataGridViewTextBoxColumn.Name = "aLAMLVLDataGridViewTextBoxColumn";
            this.aLAMLVLDataGridViewTextBoxColumn.ReadOnly = true;
            this.aLAMLVLDataGridViewTextBoxColumn.Width = 85;
            // 
            // rPTDATETIMEDataGridViewTextBoxColumn
            // 
            this.rPTDATETIMEDataGridViewTextBoxColumn.DataPropertyName = "RPT_DATE_TIME";
            this.rPTDATETIMEDataGridViewTextBoxColumn.FillWeight = 80F;
            this.rPTDATETIMEDataGridViewTextBoxColumn.HeaderText = "Happend time";
            this.rPTDATETIMEDataGridViewTextBoxColumn.Name = "rPTDATETIMEDataGridViewTextBoxColumn";
            this.rPTDATETIMEDataGridViewTextBoxColumn.ReadOnly = true;
            this.rPTDATETIMEDataGridViewTextBoxColumn.Width = 155;
            // 
            // cLEARDATETIMEDataGridViewTextBoxColumn
            // 
            this.cLEARDATETIMEDataGridViewTextBoxColumn.DataPropertyName = "CLEAR_DATE_TIME";
            this.cLEARDATETIMEDataGridViewTextBoxColumn.FillWeight = 80F;
            this.cLEARDATETIMEDataGridViewTextBoxColumn.HeaderText = "Clear Time";
            this.cLEARDATETIMEDataGridViewTextBoxColumn.Name = "cLEARDATETIMEDataGridViewTextBoxColumn";
            this.cLEARDATETIMEDataGridViewTextBoxColumn.ReadOnly = true;
            this.cLEARDATETIMEDataGridViewTextBoxColumn.Width = 135;
            // 
            // aLAMDESCDataGridViewTextBoxColumn
            // 
            this.aLAMDESCDataGridViewTextBoxColumn.DataPropertyName = "ALAM_DESC";
            this.aLAMDESCDataGridViewTextBoxColumn.HeaderText = "Description";
            this.aLAMDESCDataGridViewTextBoxColumn.Name = "aLAMDESCDataGridViewTextBoxColumn";
            this.aLAMDESCDataGridViewTextBoxColumn.ReadOnly = true;
            this.aLAMDESCDataGridViewTextBoxColumn.Width = 145;
            // 
            // aLARMObjToShowBindingSource
            // 
            this.aLARMObjToShowBindingSource.DataSource = typeof(com.mirle.ibg3k0.sc.ObjectRelay.ALARMObjToShow);
            // 
            // HistoryAlarmsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 22F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1602, 716);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Consolas", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Name = "HistoryAlarmsForm";
            this.Text = "TransferCommandQureyListForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TransferCommandQureyListForm_FormClosed);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.skinGroupBox2.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.skinGroupBox4.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.skinGroupBox1.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.skinGroupBox3.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_alarms)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.aLARMObjToShowBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.BindingSource aLARMObjToShowBindingSource;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private CCWin.SkinControl.SkinGroupBox skinGroupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.ComboBox m_EqptIDCbx;
        private CCWin.SkinControl.SkinGroupBox skinGroupBox4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.DateTimePicker m_EndDTCbx;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker m_StartDTCbx;
        private System.Windows.Forms.Label label1;
        private CCWin.SkinControl.SkinButton btnlSearch;
        private CCWin.SkinControl.SkinGroupBox skinGroupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.MaskedTextBox m_AlarmCodeTbl;
        private System.Windows.Forms.DataGridView dgv_alarms;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel2;
        private CCWin.SkinControl.SkinButton m_exportBtn;
        private CCWin.SkinControl.SkinGroupBox skinGroupBox3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox txt_cstID;
        private System.Windows.Forms.DataGridViewTextBoxColumn aLAMCODEDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn eQPTIDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aLAMSTATDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aLAMLVLDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn rPTDATETIMEDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn cLEARDATETIMEDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aLAMDESCDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CommandID1;
        private System.Windows.Forms.DataGridViewTextBoxColumn CommandID2;
        private System.Windows.Forms.DataGridViewTextBoxColumn CommandID3;
        private System.Windows.Forms.DataGridViewTextBoxColumn CommandID4;
    }
}