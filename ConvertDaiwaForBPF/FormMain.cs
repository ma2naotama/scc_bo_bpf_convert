using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConvertDaiwaForBPF
{
    public partial class FormMain : Form
    {
        private static FormMain mInstance = null;


        public FormMain()
        {
            mInstance = this;

            InitializeComponent();
        }

        public static FormMain GetInstance()
        {
            return mInstance;
        }

        public void WriteLog(String logText)
        {
            if (textBox_Log.InvokeRequired)
            {
                //別スレッドから呼び出されるとエラーになる為、スレッドセーフな処理にする
                //メッセージに(THREAD)を付け足して、再起呼び出し
                Action safeWrite = delegate { WriteLog($"(THREAD){logText}"); };
                textBox_Log.Invoke(safeWrite);
            }
            else
            {
                string str = "[" + System.DateTime.Now.ToString() + "]" + logText + "\r\n";
                textBox_Log.SelectionStart = textBox_Log.Text.Length;
                textBox_Log.SelectionLength = 0;
                textBox_Log.SelectedText = str;
                Debug.WriteLine(str);
            }
        }

        private string mOutputFileName = "";

        private void FormMain_Load(object sender, EventArgs e)
        {

            DateTime dt = DateTime.Now;

            mOutputFileName = ".\\"+String.Format("Converted_{0}.csv", dt.ToString("yyyyMMdd"));       // デフォルトファイル名

            textBox3.Text = mOutputFileName;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDialog = new FolderBrowserDialog();

            // ダイアログの説明文を指定する
            fbDialog.Description = "受領フォルダの選択";

            string stCurrentDir = System.IO.Directory.GetCurrentDirectory();

            // デフォルトのフォルダを指定する
            fbDialog.SelectedPath = stCurrentDir;

            // 「新しいフォルダーの作成する」ボタンを表示する
            fbDialog.ShowNewFolderButton = true;

            if (fbDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = fbDialog.SelectedPath;
            }

            CheckActiveRunButton();
        }


        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBox1.Text = files[0];
            }

            CheckActiveRunButton();
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            string[] Csvfilters = new string[]
            {
               "CSVファイル|*.csv"
            };

            //[ファイルの種類]に表示される選択肢を指定する
            //指定しないとすべてのファイルが表示される
            ofd.Filter = String.Join("|", Csvfilters);

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = ofd.FileName;
            }

            CheckActiveRunButton();
        }


        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBox2.Text = files[0];
            }

            CheckActiveRunButton();

        }


        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;
        }



        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            DateTime dt = DateTime.Now;

            sfd.FileName = mOutputFileName;       // デフォルトファイル名

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox3.Text = sfd.FileName;
            }

            CheckActiveRunButton();
        }

        private void textBox3_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBox3.Text = files[0];
            }

            CheckActiveRunButton();

        }



        private void textBox3_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;

        }


        bool isTextActive(string str)
        {
            if(str == null)
            {
                return false;
            }

            if(str.Length == 0)
            {
                return false;
            }

            return true;
        }

        void CheckActiveRunButton()
        {
            buttonConvert.Enabled = false;

            //if (isTextActive(textBox1.Text) && isTextActive(textBox2.Text) && isTextActive(textBox3.Text))
            if (isTextActive(textBox1.Text) && isTextActive(textBox3.Text))
            {
                    buttonConvert.Enabled = true;
            }
        }


        private ConverterMain   mConverterMain = null;


        private void buttonConvert_Click(object sender, EventArgs e)
        {
            mConverterMain = new ConverterMain();
            mConverterMain.InitConvert(
                textBox1.Text
                , textBox2.Text
                , textBox3.Text
            );

            using (FormProgressDialog dlg = new FormProgressDialog())
            {
                //マルチスレッド用のクラスを渡す
                dlg.SetThreadClass(mConverterMain);

                //プログレスバーの表示とマルチスレッドスタート
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show(Properties.Resources.MSG_CONVERT_FINISHED.ToString());
                }
                else
                {
                    //MessageBox.Show("キャンセルしました。");
                    return;
                }

            }

        }

    }
}
