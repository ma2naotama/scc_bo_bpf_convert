using System;
using System.Windows.Forms;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 起動画面
    /// </summary>
    public partial class FormMain : Form
    {
        /// <summary>
        /// FormMainのインスタンス
        /// </summary>
        private static FormMain mInstance = null;

        /// <summary>
        /// ConverterMainのインスタンス
        /// </summary>
        private ConverterMain mConverterMain = null;

        /// <summary>
        /// 健診メイン
        /// </summary>
        public FormMain()
        {
            mInstance = this;

            InitializeComponent();
        }

        /// <summary>
        /// 健診メインのフォームの読み込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_Load(object sender, EventArgs e)
        {

            textBoxOutputPath.Text = "..\\";
        }

        /// <summary>
        /// 起動画面のインスタンスの取得
        /// </summary>
        /// <returns></returns>
        public static FormMain GetInstance()
        {
            return mInstance;
        }

        /// <summary>
        /// ログ画面出力（スレッド対応）
        /// </summary>
        /// <param name="logText"></param>
        public void ViewLog(String logText)
        {
            if (textBox_Log.InvokeRequired)
            {
                //別スレッドから呼び出されるとエラーになる為、スレッドセーフな処理にする
                //再起呼び出し
                Action safeWrite = delegate { ViewLog($"{logText}"); };
                textBox_Log.Invoke(safeWrite);
            }
            else
            {
                string str = "[" + System.DateTime.Now.ToString() + "]" + logText + "\r\n";
                textBox_Log.SelectionStart = textBox_Log.Text.Length;
                textBox_Log.SelectionLength = 0;
                textBox_Log.SelectedText = str;
            }
        }


        /// <summary>
        /// 受領フォルダパスの選択ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonReceivePath_Click(object sender, EventArgs e)
        {
            var fbDialog = new FolderBrowserDialog();

            // ダイアログの説明文を指定する
            fbDialog.Description = "受領フォルダの選択";

            string stCurrentDir = System.IO.Directory.GetCurrentDirectory();

            // デフォルトのフォルダを指定する
            fbDialog.SelectedPath = stCurrentDir;

            // 「新しいフォルダーの作成する」ボタンを表示する
            fbDialog.ShowNewFolderButton = true;

            if (fbDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxReceivePath.Text = fbDialog.SelectedPath;

                // キャレットを文字列の最後に移動
                textBoxReceivePath.Select(textBoxReceivePath.Text.Length, 0);
            }

            CheckActiveRunButton();
        }


        /// <summary>
        /// 受領フォルダパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxReceivePath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBoxReceivePath.Text = files[0];

                // キャレットを文字列の最後に移動
                textBoxReceivePath.Select(textBoxReceivePath.Text.Length, 0);
            }

            CheckActiveRunButton();
        }

        /// <summary>
        /// 受領フォルダパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxReceivePath_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;

        }

        /// <summary>
        /// 受領フォルダパスの内容変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxReceivePath_TextChanged(object sender, EventArgs e)
        {
            CheckActiveRunButton();
        }


        /// <summary>
        /// 人事パスの選択ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonHRPath_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            var Csvfilters = new string[]
            {
               "CSVファイル|*.csv"
            };

            //[ファイルの種類]に表示される選択肢を指定する
            //指定しないとすべてのファイルが表示される
            ofd.Filter = String.Join("|", Csvfilters);

            string stCurrentDir = System.IO.Directory.GetCurrentDirectory();

            // デフォルトのフォルダを指定する
            ofd.InitialDirectory = stCurrentDir;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBoxHRPath.Text = ofd.FileName;

                // キャレットを文字列の最後に移動
                textBoxHRPath.Select(textBoxHRPath.Text.Length, 0);
            }

            CheckActiveRunButton();
        }

        /// <summary>
        /// 人事パスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxHRPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBoxHRPath.Text = files[0];

                // キャレットを文字列の最後に移動
                textBoxHRPath.Select(textBoxHRPath.Text.Length, 0);
            }

            CheckActiveRunButton();

        }

        /// <summary>
        /// 人事パスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxHRPath_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// 人事パスの内容変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxHRPath_TextChanged(object sender, EventArgs e)
        {
            CheckActiveRunButton();
        }

        /// <summary>
        /// 出力フォルダパスの選択ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOutputPath_Click(object sender, EventArgs e)
        {

            var fbDialog = new FolderBrowserDialog();

            // ダイアログの説明文を指定する
            fbDialog.Description = "出力フォルダの選択";

            var stCurrentDir = System.IO.Directory.GetCurrentDirectory();

            // デフォルトのフォルダを指定する
            fbDialog.SelectedPath = stCurrentDir;

            // 「新しいフォルダーの作成する」ボタンを表示する
            fbDialog.ShowNewFolderButton = true;

            if (fbDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxOutputPath.Text = fbDialog.SelectedPath;

                // キャレットを文字列の最後に移動
                textBoxOutputPath.Select(textBoxOutputPath.Text.Length, 0);
            }

            CheckActiveRunButton();
        }

        /// <summary>
        /// 出力フォルダパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxOutputPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBoxOutputPath.Text = files[0];

                // キャレットを文字列の最後に移動
                textBoxOutputPath.Select(textBoxOutputPath.Text.Length, 0);
            }

            CheckActiveRunButton();
        }

        /// <summary>
        /// 出力フォルダパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxOutputPath_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;

        }

        /// <summary>
        /// 出力フォルダパスの内容変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxOutputPath_TextChanged(object sender, EventArgs e)
        {
            CheckActiveRunButton();
        }

        /// <summary>
        /// 変換処理の表示非表示の判定
        /// </summary>
        private void CheckActiveRunButton()
        {
            //実行ボタンの非表示
            buttonConvert.Enabled = false;

            if (!string.IsNullOrEmpty(textBoxReceivePath.Text) && !string.IsNullOrEmpty(textBoxHRPath.Text) && !string.IsNullOrEmpty(textBoxOutputPath.Text))
            {
                //実行ボタンの表示
                buttonConvert.Enabled = true;
            }
        }

        /// <summary>
        /// 変換処理実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConvert_Click(object sender, EventArgs e)
        {
            mConverterMain = new ConverterMain();
            mConverterMain.InitConvert(
                textBoxReceivePath.Text
                , textBoxHRPath.Text
                , textBoxOutputPath.Text
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
            }

        }

    }
}
