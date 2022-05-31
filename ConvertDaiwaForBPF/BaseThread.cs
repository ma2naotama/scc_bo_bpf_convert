using System;
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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BaseThread()
        {
        }

        /// <summary>
        /// マルチスレッドの処理
        /// </summary>
        /// <returns></returns>
        public abstract bool MultiThreadMethod(CancellationToken ct);


        /// <summary>
        /// 非同期処理開始
        /// </summary>
        public void RunMultiThreadAsync()
        {
            // 変数初期化
            Cancel = false;
            Completed = false;

            // キャンセルトークンソースを生成し、キャンセルトークンを取得します。
            mTtokenSource = new CancellationTokenSource();
            var ct = mTtokenSource.Token;

            // 非同期処理（マルチスレッド）開始
            try
            {
                //Dbg.Log("RunMultiThread");

                var task = Task.Run(() =>
                {
                    // Were we already canceled?
                    ct.ThrowIfCancellationRequested();

                    if(!MultiThreadMethod(ct))
                    {
                        // プログラム上でキャンセルとなった場合
                        Dbg.ViewLog(Properties.Resources.MSG_CONVERT_CANCEL);

                        // Taskを終了する.
                        Cancel = true;
                        return;
                    }

                }, ct);

            }
            catch (TaskCanceledException ex)
            {
                // キャンセルされた場合の例外処理
                Dbg.Debug(Properties.Resources.E_TASK_CANCEL_ERROR);

                throw ex;
            }
            catch (Exception ex)
            {
                // 異常終了した場合の例外処理
                Dbg.ErrorWithView(Properties.Resources.E_TASK_ERROR);

                throw ex;
            }
        }

        /// <summary>
        /// スレッドのキャンセル
        /// </summary>
        /// <returns>bool
        /// true    ;キャンセル処理正常
        /// false   :キャンセル処理異常
        /// </returns>
        public virtual bool MultiThreadCancel()
        {
            if (mTtokenSource != null)
            {
                mTtokenSource.Cancel();
                mTtokenSource = null;

                return true;
            }

            return false;
        }

    }
}
