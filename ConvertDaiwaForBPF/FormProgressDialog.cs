using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConvertDaiwaForBPF
{
    public partial class FormProgressDialog : Form
    {
        BaseThread _base = null;


        public FormProgressDialog()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.progressBar1.MarqueeAnimationSpeed = 30;
            this.progressBar1.Style = ProgressBarStyle.Marquee;

            //マルチスレッドのクラスがない場合は、何もしないで閉じる
            if (_base == null)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            //タイマー開始
            this.timer1.Enabled = true;
            this.timer1.Interval = 1;

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

        private void timer1_Tick(object sender, EventArgs e)
        {
            //終了判定
            if (_base.Cancel)
            {
                //タイマーの停止
                this.timer1.Stop();

                Dbg.Log("timer1_Tick Cancel:" + _base.Cancel);

                //_base.MultiThreadCancel();

                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            if (_base.Completed)
            {
                //タイマーの停止
                this.timer1.Stop();
                //Dbg.Log("timer1_Tick Completed:" + _base.Completed);

                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

        }

        //フォームを閉じた時に呼ばれる。（フォームの×ボタンでもthis.Close()を実行でも呼ばれる）
        private void FormProgressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            Dbg.Log("From Close");
            if (!_base.Completed)
            {
                Dbg.Log("変換キャンセル");
                _base.MultiThreadCancel();
            } 
        }
    }
}
