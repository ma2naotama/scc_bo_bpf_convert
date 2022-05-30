using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    /// <summary>
    /// スレッドの基礎クラス
    /// </summary>
    public abstract class BaseThread
    {
        /// <summary>
        /// キャンセルフラグ
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 完了フラグ
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// キャンセル用トークン
        /// </summary>
        private CancellationTokenSource mTtokenSource = null;

        // コンストラクタ
        public BaseThread()
        {
        }

        /// <summary>
        /// マルチスレッドの処理
        /// </summary>
        /// <returns></returns>
        public  abstract int MultiThreadMethod();


        /// <summary>
        /// 非同期処理開始
        /// </summary>
        public void RunMultiThreadAsync()
        {
            // 変数初期化
            Cancel = false;
            Completed = false;

            // キャンセルトークンソースを生成し、キャンセルトークンを取得します。
            if (mTtokenSource == null) 
                mTtokenSource = new CancellationTokenSource();

            // 非同期処理（マルチスレッド）開始
            try
            {
                //Dbg.Log("RunMultiThread");

                var task = Task.Factory.StartNew(() =>
                {
                    if(MultiThreadMethod() == 0)
                    {
                        Cancel=true;
                    }
                }, mTtokenSource.Token);

            }
            catch (TaskCanceledException ex)
            {
                // キャンセルされた場合の例外処理
                Dbg.Debug(Properties.Resources.E_TASK_CANCEL_ERROR + ex.ToString());
            }
            catch (Exception ex)
            {
                // 異常終了した場合の例外処理
                Dbg.ErrorWithView(Properties.Resources.E_TASK_ERROR + ex.ToString());

                throw ex;
            }

        }

        /// <summary>
        /// スレッドのキャンセル
        /// </summary>
        public virtual void MultiThreadCancel()
        {
            Dbg.ViewLog(Properties.Resources.MSG_CONVERT_CANCEL);

            if (mTtokenSource != null)
            {
                mTtokenSource.Cancel();
                mTtokenSource = null;
            } 

            Cancel = true;
        }

    }
}
