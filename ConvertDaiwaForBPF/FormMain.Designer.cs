namespace ConvertDaiwaForBPF
{
    partial class FormMain
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_Log = new System.Windows.Forms.TextBox();
            this.buttonReceivePath = new System.Windows.Forms.Button();
            this.textBoxHRPath = new System.Windows.Forms.TextBox();
            this.buttonHRPath = new System.Windows.Forms.Button();
            this.textBoxOutputPath = new System.Windows.Forms.TextBox();
            this.buttonOutputPath = new System.Windows.Forms.Button();
            this.buttonConvert = new System.Windows.Forms.Button();
            this.labelReceiveFolder = new System.Windows.Forms.Label();
            this.labelHR = new System.Windows.Forms.Label();
            this.labelOutput = new System.Windows.Forms.Label();
            this.textBoxReceivePath = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox_Log
            // 
            this.textBox_Log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_Log.Location = new System.Drawing.Point(7, 301);
            this.textBox_Log.Multiline = true;
            this.textBox_Log.Name = "textBox_Log";
            this.textBox_Log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox_Log.Size = new System.Drawing.Size(647, 172);
            this.textBox_Log.TabIndex = 0;
            // 
            // buttonReceivePath
            // 
            this.buttonReceivePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonReceivePath.Location = new System.Drawing.Point(595, 38);
            this.buttonReceivePath.Name = "buttonReceivePath";
            this.buttonReceivePath.Size = new System.Drawing.Size(46, 22);
            this.buttonReceivePath.TabIndex = 4;
            this.buttonReceivePath.Text = "選択";
            this.buttonReceivePath.UseVisualStyleBackColor = true;
            this.buttonReceivePath.Click += new System.EventHandler(this.buttonReceivePath_Click);
            // 
            // textBoxHRPath
            // 
            this.textBoxHRPath.AllowDrop = true;
            this.textBoxHRPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxHRPath.Font = new System.Drawing.Font("ＭＳ ゴシック", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBoxHRPath.Location = new System.Drawing.Point(21, 99);
            this.textBoxHRPath.Name = "textBoxHRPath";
            this.textBoxHRPath.Size = new System.Drawing.Size(568, 20);
            this.textBoxHRPath.TabIndex = 5;
            this.textBoxHRPath.TextChanged += new System.EventHandler(this.textBoxHRPath_TextChanged);
            this.textBoxHRPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBoxHRPath_DragDrop);
            this.textBoxHRPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.textBoxHRPath_DragEnter);
            // 
            // buttonHRPath
            // 
            this.buttonHRPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonHRPath.Location = new System.Drawing.Point(595, 100);
            this.buttonHRPath.Name = "buttonHRPath";
            this.buttonHRPath.Size = new System.Drawing.Size(46, 22);
            this.buttonHRPath.TabIndex = 6;
            this.buttonHRPath.Text = "選択";
            this.buttonHRPath.UseVisualStyleBackColor = true;
            this.buttonHRPath.Click += new System.EventHandler(this.buttonHRPath_Click);
            // 
            // textBoxOutputPath
            // 
            this.textBoxOutputPath.AllowDrop = true;
            this.textBoxOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutputPath.Font = new System.Drawing.Font("ＭＳ ゴシック", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBoxOutputPath.Location = new System.Drawing.Point(21, 159);
            this.textBoxOutputPath.Name = "textBoxOutputPath";
            this.textBoxOutputPath.Size = new System.Drawing.Size(568, 20);
            this.textBoxOutputPath.TabIndex = 7;
            this.textBoxOutputPath.TextChanged += new System.EventHandler(this.textBoxOutputPath_TextChanged);
            this.textBoxOutputPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBoxOutputPath_DragDrop);
            this.textBoxOutputPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.textBoxOutputPath_DragEnter);
            // 
            // buttonOutputPath
            // 
            this.buttonOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOutputPath.Location = new System.Drawing.Point(595, 159);
            this.buttonOutputPath.Name = "buttonOutputPath";
            this.buttonOutputPath.Size = new System.Drawing.Size(46, 22);
            this.buttonOutputPath.TabIndex = 8;
            this.buttonOutputPath.Text = "選択";
            this.buttonOutputPath.UseVisualStyleBackColor = true;
            this.buttonOutputPath.Click += new System.EventHandler(this.buttonOutputPath_Click);
            // 
            // buttonConvert
            // 
            this.buttonConvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonConvert.Enabled = false;
            this.buttonConvert.Location = new System.Drawing.Point(514, 215);
            this.buttonConvert.Name = "buttonConvert";
            this.buttonConvert.Size = new System.Drawing.Size(127, 54);
            this.buttonConvert.TabIndex = 9;
            this.buttonConvert.Text = "実行";
            this.buttonConvert.UseVisualStyleBackColor = true;
            this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
            // 
            // labelReceiveFolder
            // 
            this.labelReceiveFolder.AutoSize = true;
            this.labelReceiveFolder.Location = new System.Drawing.Point(26, 22);
            this.labelReceiveFolder.Name = "labelReceiveFolder";
            this.labelReceiveFolder.Size = new System.Drawing.Size(76, 12);
            this.labelReceiveFolder.TabIndex = 10;
            this.labelReceiveFolder.Text = "【受領フォルダ】";
            // 
            // labelHR
            // 
            this.labelHR.AutoSize = true;
            this.labelHR.Location = new System.Drawing.Point(25, 85);
            this.labelHR.Name = "labelHR";
            this.labelHR.Size = new System.Drawing.Size(69, 12);
            this.labelHR.TabIndex = 11;
            this.labelHR.Text = "【人事データ】";
            // 
            // labelOutput
            // 
            this.labelOutput.AutoSize = true;
            this.labelOutput.Location = new System.Drawing.Point(26, 144);
            this.labelOutput.Name = "labelOutput";
            this.labelOutput.Size = new System.Drawing.Size(53, 12);
            this.labelOutput.TabIndex = 12;
            this.labelOutput.Text = "【出力先】";
            // 
            // textBoxReceivePath
            // 
            this.textBoxReceivePath.AllowDrop = true;
            this.textBoxReceivePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxReceivePath.Font = new System.Drawing.Font("ＭＳ ゴシック", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBoxReceivePath.Location = new System.Drawing.Point(21, 37);
            this.textBoxReceivePath.Name = "textBoxReceivePath";
            this.textBoxReceivePath.Size = new System.Drawing.Size(568, 20);
            this.textBoxReceivePath.TabIndex = 13;
            this.textBoxReceivePath.TextChanged += new System.EventHandler(this.textBoxReceivePath_TextChanged);
            this.textBoxReceivePath.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBoxReceivePath_DragDrop);
            this.textBoxReceivePath.DragEnter += new System.Windows.Forms.DragEventHandler(this.textBoxReceivePath_DragEnter);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(666, 485);
            this.Controls.Add(this.textBoxReceivePath);
            this.Controls.Add(this.labelOutput);
            this.Controls.Add(this.labelHR);
            this.Controls.Add(this.labelReceiveFolder);
            this.Controls.Add(this.buttonConvert);
            this.Controls.Add(this.buttonOutputPath);
            this.Controls.Add(this.textBoxOutputPath);
            this.Controls.Add(this.buttonHRPath);
            this.Controls.Add(this.textBoxHRPath);
            this.Controls.Add(this.buttonReceivePath);
            this.Controls.Add(this.textBox_Log);
            this.Name = "FormMain";
            this.Text = "健診結果取込フォーマット作成ツール";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Log;
        private System.Windows.Forms.Button buttonReceivePath;
        private System.Windows.Forms.TextBox textBoxHRPath;
        private System.Windows.Forms.Button buttonHRPath;
        private System.Windows.Forms.TextBox textBoxOutputPath;
        private System.Windows.Forms.Button buttonOutputPath;
        private System.Windows.Forms.Button buttonConvert;
        private System.Windows.Forms.Label labelReceiveFolder;
        private System.Windows.Forms.Label labelHR;
        private System.Windows.Forms.Label labelOutput;
        private System.Windows.Forms.TextBox textBoxReceivePath;
    }
}

