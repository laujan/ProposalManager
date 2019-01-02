namespace SourcePointCreator
{
    partial class frm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnCreateCatalog = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtCatalog = new System.Windows.Forms.TextBox();
            this.cboCatalog = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.grdPoints = new System.Windows.Forms.DataGridView();
            this.Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Remove = new System.Windows.Forms.DataGridViewButtonColumn();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.radImage = new System.Windows.Forms.RadioButton();
            this.btnCreate = new System.Windows.Forms.Button();
            this.radTable = new System.Windows.Forms.RadioButton();
            this.radPoint = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.txtPointName = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdPoints)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnCreateCatalog);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtCatalog);
            this.groupBox1.Controls.Add(this.cboCatalog);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(334, 121);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Catalogs";
            // 
            // btnCreateCatalog
            // 
            this.btnCreateCatalog.Location = new System.Drawing.Point(253, 87);
            this.btnCreateCatalog.Name = "btnCreateCatalog";
            this.btnCreateCatalog.Size = new System.Drawing.Size(75, 23);
            this.btnCreateCatalog.TabIndex = 8;
            this.btnCreateCatalog.Text = "Create";
            this.btnCreateCatalog.UseVisualStyleBackColor = true;
            this.btnCreateCatalog.Click += new System.EventHandler(this.btnCreateCatalog_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 45);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Name:";
            // 
            // txtCatalog
            // 
            this.txtCatalog.Location = new System.Drawing.Point(7, 61);
            this.txtCatalog.Name = "txtCatalog";
            this.txtCatalog.Size = new System.Drawing.Size(321, 20);
            this.txtCatalog.TabIndex = 4;
            // 
            // cboCatalog
            // 
            this.cboCatalog.DisplayMember = "Name";
            this.cboCatalog.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCatalog.FormattingEnabled = true;
            this.cboCatalog.Location = new System.Drawing.Point(7, 20);
            this.cboCatalog.Name = "cboCatalog";
            this.cboCatalog.Size = new System.Drawing.Size(321, 21);
            this.cboCatalog.TabIndex = 0;
            this.cboCatalog.SelectedIndexChanged += new System.EventHandler(this.cboCatalog_SelectedIndexChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.grdPoints);
            this.groupBox2.Location = new System.Drawing.Point(353, 22);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(439, 289);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Points";
            // 
            // grdPoints
            // 
            this.grdPoints.AllowUserToAddRows = false;
            this.grdPoints.AllowUserToDeleteRows = false;
            this.grdPoints.AllowUserToResizeRows = false;
            this.grdPoints.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdPoints.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Name,
            this.Value,
            this.Type,
            this.Remove});
            this.grdPoints.Location = new System.Drawing.Point(7, 20);
            this.grdPoints.MultiSelect = false;
            this.grdPoints.Name = "grdPoints";
            this.grdPoints.ReadOnly = true;
            this.grdPoints.RowHeadersVisible = false;
            this.grdPoints.Size = new System.Drawing.Size(423, 263);
            this.grdPoints.TabIndex = 0;
            this.grdPoints.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdPoints_CellContentClick);
            // 
            // Name
            // 
            this.Name.DataPropertyName = "Name";
            this.Name.HeaderText = "Name";
            this.Name.Name = "Name";
            this.Name.ReadOnly = true;
            // 
            // Value
            // 
            this.Value.DataPropertyName = "Value";
            this.Value.HeaderText = "Value";
            this.Value.Name = "Value";
            this.Value.ReadOnly = true;
            // 
            // Type
            // 
            this.Type.DataPropertyName = "SourceType";
            this.Type.HeaderText = "Type";
            this.Type.Name = "Type";
            this.Type.ReadOnly = true;
            // 
            // Remove
            // 
            this.Remove.HeaderText = "Remove";
            this.Remove.Name = "Remove";
            this.Remove.ReadOnly = true;
            this.Remove.Text = "X";
            this.Remove.UseColumnTextForButtonValue = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radImage);
            this.groupBox3.Controls.Add(this.btnCreate);
            this.groupBox3.Controls.Add(this.radTable);
            this.groupBox3.Controls.Add(this.radPoint);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.txtValue);
            this.groupBox3.Controls.Add(this.txtPointName);
            this.groupBox3.Location = new System.Drawing.Point(13, 140);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(335, 212);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "New point";
            // 
            // radImage
            // 
            this.radImage.AutoSize = true;
            this.radImage.Location = new System.Drawing.Point(201, 27);
            this.radImage.Name = "radImage";
            this.radImage.Size = new System.Drawing.Size(54, 17);
            this.radImage.TabIndex = 17;
            this.radImage.Text = "Image";
            this.radImage.UseVisualStyleBackColor = true;
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(252, 178);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(75, 23);
            this.btnCreate.TabIndex = 16;
            this.btnCreate.Text = "Add";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // radTable
            // 
            this.radTable.AutoSize = true;
            this.radTable.Location = new System.Drawing.Point(111, 27);
            this.radTable.Name = "radTable";
            this.radTable.Size = new System.Drawing.Size(52, 17);
            this.radTable.TabIndex = 15;
            this.radTable.Text = "Table";
            this.radTable.UseVisualStyleBackColor = true;
            // 
            // radPoint
            // 
            this.radPoint.AutoSize = true;
            this.radPoint.Checked = true;
            this.radPoint.Location = new System.Drawing.Point(10, 27);
            this.radPoint.Name = "radPoint";
            this.radPoint.Size = new System.Drawing.Size(49, 17);
            this.radPoint.TabIndex = 14;
            this.radPoint.TabStop = true;
            this.radPoint.Text = "Point";
            this.radPoint.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 11);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Type:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 86);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Value:";
            // 
            // txtValue
            // 
            this.txtValue.Location = new System.Drawing.Point(7, 105);
            this.txtValue.Multiline = true;
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(321, 66);
            this.txtValue.TabIndex = 10;
            // 
            // txtPointName
            // 
            this.txtPointName.Location = new System.Drawing.Point(7, 63);
            this.txtPointName.Name = "txtPointName";
            this.txtPointName.Size = new System.Drawing.Size(321, 20);
            this.txtPointName.TabIndex = 9;
            // 
            // frm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(798, 361);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Text = "Source Point Creator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdPoints)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cboCatalog;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnCreateCatalog;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtCatalog;
        private System.Windows.Forms.DataGridView grdPoints;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton radImage;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.RadioButton radTable;
        private System.Windows.Forms.RadioButton radPoint;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.TextBox txtPointName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn Value;
        private System.Windows.Forms.DataGridViewTextBoxColumn Type;
        private System.Windows.Forms.DataGridViewButtonColumn Remove;
    }
}

