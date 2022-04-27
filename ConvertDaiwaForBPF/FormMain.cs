using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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


        private void FormMain_Load(object sender, EventArgs e)
        {
            ReadMasterFile();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            string[] filters = new string[]
            {
                "CSVファイル|*.csv"
            };

            //[ファイルの種類]に表示される選択肢を指定する
            //指定しないとすべてのファイルが表示される
            ofd.Filter = String.Join("|", filters);

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBox1.Text = files[0];
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;

        }


        Dictionary<string, DataTable> mMasterSheets = null;

        private void ReadMasterFile()
        {
            if(mMasterSheets != null)
            {
                //2重読み込み防止
                return;
            }

            BaseExcelLoader excel = new BaseExcelLoader();

            ExcelOption[] optionarray = new ExcelOption[]
            {
                new ExcelOption ( "DHPTV001HED",        2, 1),
                new ExcelOption ( "DHPTV001DTL",        2, 1),
                new ExcelOption ( "JLAC10変換",         2, 1),
                new ExcelOption ( "項目マッピング",     2, 1),
                new ExcelOption ( "コードマッピング",   2, 1),
                new ExcelOption ( "ロジックマッピング", 2, 1),
                new ExcelOption ( "オーダーマッピング", 2, 1),
            };

            excel.SetExcelOptionArray(optionarray);

            mMasterSheets = excel.ReadAllSheets(".\\_master\\master.xlsm");

            DataTable sheet = mMasterSheets["項目マッピング"];

            /*
             * 検索のサンプル
            DataRow[] rows =
                sheet.AsEnumerable()
                  .Where(x => Int32.Parse(x["No"].ToString()) > 1)
                  .ToArray();
            */

            /*
            DataRow[] rows =
            sheet.AsEnumerable()
              .Where(x => x["項目名"].ToString() != "")
              .ToArray();

            foreach (DataRow row in rows)
                Dbg.Log(row["項目名"].ToString());
            */

        }




    }
}
