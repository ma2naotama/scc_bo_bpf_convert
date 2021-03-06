using System;
using System.Security.Permissions;
using System.Windows.Forms;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// 変換中ダイアログ
    /// </summary>
    public partial class FormProgressDialog : Form
    {
        /// <summary>
        /// マルチスレッド
        /// </summary>
        private BaseThread mBase = null;

        const string TITLE_MESSAGE_START = "変換中";
        const string TITLE_MESSAGE_CANCELING = "キャンセル中...";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FormProgressDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// フォームのCreateParamsプロパティをオーバーライド
        /// 「閉じる」ボタンが無効状態となり、押すことができなくなります
        /// </summary>
        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.Demand,
                Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                const int CS_NOCLOSE = 0x200;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_NOCLOSE;

                return cp;
            }
        }

        /// <summary>
        /// 変換中ダイアログの読み込み
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Text = TITLE_MESSAGE_START;

            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Style = ProgressBarStyle.Marquee;

            // マルチスレッドのクラスがない場合は、何もしないで閉じる
            if (mBase == null)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            // タイマー開始
            timerProgress.Enabled = true;
            timerProgress.Interval = 1;

            // マルチスレッドスタート
            mBase.RunMultiThreadAsync();
        }

        /// <summary>
        /// 変換中ダイアログの読み込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormProgressDialog_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// BaseThreadClassを継承したクラスを取得
        /// </summary>
        /// <param name="thread"></param>
        public void SetThreadClass(BaseThread thread)
        {
            mBase = thread;
        }

        /// <summary>
        /// タイマーティック処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerProgress_Tick(object sender, EventArgs e)
        {
            // 終了判定
            if (mBase.Cancel)
            {
                // タイマーの停止
                timerProgress.Stop();

                // キャンセルボタンの表示
                buttonCancel.Enabled = true;

                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (mBase.Completed)
            {
                // タイマーの停止
                timerProgress.Stop();

                // キャンセルボタンの表示
                buttonCancel.Enabled = true;

                DialogResult = DialogResult.OK;
                Close();
                return;
            }
        }

        /// <summary>
        /// フォームを閉じた時に呼ばれる。（フォームの×ボタンでもthis.Close()を実行でも呼ばれる）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormProgressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// キャンセルボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            //Dbg.Log("From Close");
            if (!mBase.Completed)
            {
                //Dbg.Log("buttonCancel_Click");
                if (mBase.MultiThreadCancel())
                {
                    // キャンセル処理が正常に実行されたら、キャンセルボタンを非表示にする
                    buttonCancel.Enabled = false;

                    Text = TITLE_MESSAGE_CANCELING;
                }
            }
        }
    }
}
