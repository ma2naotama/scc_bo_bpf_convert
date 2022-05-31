using System;
using System.Security.Permissions;
using System.Windows.Forms;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// ダイアログ表示
    /// </summary>
    public partial class FormProgressDialog : Form
    {
        private BaseThread _base = null;

        public FormProgressDialog()
        {
            InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.Demand,
                Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                const int CS_NOCLOSE = 0x200;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle = cp.ClassStyle | CS_NOCLOSE;

                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            progressBar1.MarqueeAnimationSpeed = 30;
            progressBar1.Style = ProgressBarStyle.Marquee;

            //マルチスレッドのクラスがない場合は、何もしないで閉じる
            if (_base == null)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            //タイマー開始
            timerProgress.Enabled = true;
            timerProgress.Interval = 1;

            //マルチスレッドスタート
            _base.RunMultiThreadAsync();
        }
      

        private void FormProgressDialog_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// BaseThreadClassを継承したクラスを取得
        /// </summary>
        /// <param name="thread"></param>
        public void SetThreadClass(BaseThread thread)
        {
            _base = thread;
        }

        /// <summary>
        /// タイマーティック処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //終了判定
            if (_base.Cancel)
            {
                //タイマーの停止
                timerProgress.Stop();

                //Dbg.ViewLog("timer1_Tick Cancel:" + _base.Cancel);

                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (_base.Completed)
            {
                //タイマーの停止
                timerProgress.Stop();
                //Dbg.Log("timer1_Tick Completed:" + _base.Completed);

                DialogResult = DialogResult.OK;
                Close();
                return;
            }

        }

        //フォームを閉じた時に呼ばれる。（フォームの×ボタンでもthis.Close()を実行でも呼ばれる）
        private void FormProgressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            //Dbg.Log("From Close");
            if (!_base.Completed)
            {
                //Dbg.Log("buttonCancel_Click");
                _base.MultiThreadCancel();
            }
        }
    }
}
