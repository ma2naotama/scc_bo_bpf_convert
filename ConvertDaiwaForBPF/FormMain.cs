using System;
using System.Windows.Forms;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 健診メインフォーム
    /// </summary>
    public partial class FormMain : Form
    {
        /// <summary>
        /// 健診メインフォームのインスタンス
        /// </summary>
        private static FormMain mInstance = null;

        /// <summary>
        /// 変換処理スレッドのインスタンス
        /// </summary>
        private ConverterMain mConverterMain = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FormMain()
        {
            mInstance = this;

            InitializeComponent();
        }

        /// <summary>
        /// ロード処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_Load(object sender, EventArgs e)
        {
            textBoxOutputPath.Text = "..\\";
        }

        /// <summary>
        /// 健診メインフォームのインスタンス取得
        /// </summary>
        /// <returns>FormMain 健診メインのインスタンス</returns>
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
                // 別スレッドから呼び出されるとエラーになる為、スレッドセーフな処理にする
                // 再起呼び出し
                Action safeWrite = delegate { ViewLog($"{logText}"); };

                textBox_Log.Invoke(safeWrite);
            }
            else
            {
                var str = "[" + DateTime.Now.ToString() + "]" + logText + "\r\n";

                textBox_Log.SelectionStart = textBox_Log.Text.Length;

                textBox_Log.SelectionLength = 0;

                textBox_Log.SelectedText = str;
            }
        }

        /// <summary>
        /// 受領フォルダ選択ボタンの押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonReceivePath_Click(object sender, EventArgs e)
        {
            var fbDialog = new FolderBrowserDialog
            {
                // ダイアログの説明文を指定する
                Description = "受領フォルダの選択"
            };

            var stCurrentDir = System.IO.Directory.GetCurrentDirectory();

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

            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }


        /// <summary>
        /// 受領フォルダパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxReceivePath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                textBoxReceivePath.Text = files[0];

                // キャレットを文字列の最後に移動
                textBoxReceivePath.Select(textBoxReceivePath.Text.Length, 0);
            }

            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 受領フォルダパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxReceivePath_DragEnter(object sender, DragEventArgs e)
        {
            //ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// 受領フォルダパスの内容変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxReceivePath_TextChanged(object sender, EventArgs e)
        {
            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 人事データ選択ボタンの押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonHRPath_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            var Csvfilters = new string[]
            {
               "CSVファイル|*.csv"
            };

            // [ファイルの種類]に表示される選択肢を指定する
            // 指定しないとすべてのファイルが表示される
            ofd.Filter = String.Join("|", Csvfilters);

            var stCurrentDir = System.IO.Directory.GetCurrentDirectory();

            // デフォルトのフォルダを指定する
            ofd.InitialDirectory = stCurrentDir;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBoxHRPath.Text = ofd.FileName;

                // キャレットを文字列の最後に移動
                textBoxHRPath.Select(textBoxHRPath.Text.Length, 0);
            }

            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 人事データパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxHRPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBoxHRPath.Text = files[0];

                // キャレットを文字列の最後に移動
                textBoxHRPath.Select(textBoxHRPath.Text.Length, 0);
            }

            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 人事データパスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxHRPath_DragEnter(object sender, DragEventArgs e)
        {
            // ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// 人事データパスの内容変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxHRPath_TextChanged(object sender, EventArgs e)
        {
            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 出力先選択ボタンの押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOutputPath_Click(object sender, EventArgs e)
        {
            var fbDialog = new FolderBrowserDialog()
            {
                // ダイアログの説明文を指定する
                Description = "出力フォルダの選択"
            };

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

            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 出力先パスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxOutputPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                textBoxOutputPath.Text = files[0];

                // キャレットを文字列の最後に移動
                textBoxOutputPath.Select(textBoxOutputPath.Text.Length, 0);
            }

            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 出力先パスのマウスドラッグイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxOutputPath_DragEnter(object sender, DragEventArgs e)
        {
            // ファイルがドラッグされたとき、カーソルをドラッグ中のアイコンに変更し、そうでない場合は何もしない。
            e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// 出力先パスの内容変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxOutputPath_TextChanged(object sender, EventArgs e)
        {
            // 実行ボタン活性／非活性の設定
            SetEnabledConvertButton();
        }

        /// <summary>
        /// 実行ボタン活性／非活性の設定
        /// </summary>
        private void SetEnabledConvertButton()
        {
            // 受領フォルダ入力済みかつ、
            // 人事データ入力済みかつ、
            // 出力先入力済み
            if (!string.IsNullOrEmpty(textBoxReceivePath.Text) &&
                !string.IsNullOrEmpty(textBoxHRPath.Text) &&
                !string.IsNullOrEmpty(textBoxOutputPath.Text))
            {
                // 実行ボタンの活性
                buttonConvert.Enabled = true;
            }
            else
            {
                // 実行ボタンの非活性
                buttonConvert.Enabled = false;
            }
        }

        /// <summary>
        /// 実行ボタンの押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonConvert_Click(object sender, EventArgs e)
        {
            mConverterMain = new ConverterMain();

            mConverterMain.InitConvert(textBoxReceivePath.Text, textBoxHRPath.Text, textBoxOutputPath.Text);

            using (FormProgressDialog dlg = new FormProgressDialog())
            {
                // マルチスレッド用のクラスを渡す
                dlg.SetThreadClass(mConverterMain);

                // プログレスバーの表示とマルチスレッドスタート
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show(Properties.Resources.MSG_CONVERT_FINISHED.ToString());
                }
            }
        }
    }
}
