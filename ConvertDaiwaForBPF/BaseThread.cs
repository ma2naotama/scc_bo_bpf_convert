using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    public abstract class BaseThread
    {
        public bool Cancel { get; set; }            //キャンセルフラグ
        public bool Completed { get; set; }         //完了フラグ


        //コンストラクタ
        public BaseThread()
        {
        }

        //マルチスレッドの処理
        public  abstract int MultiThreadMethod();


        private CancellationTokenSource _tokenSource = null;


        //非同期処理開始
        public void RunMultiThreadAsync()
        {
            //変数初期化
            Cancel = false;
            Completed = false;

             // キャンセルトークンソースを生成し、キャンセルトークンを取得します。
            if (_tokenSource == null) 
                _tokenSource = new CancellationTokenSource();

            //非同期処理（マルチスレッド）開始
            try
            {
                Dbg.Log("RunMultiThread");

                var task = Task.Factory.StartNew(() =>
                {
                    MultiThreadMethod();
                }, _tokenSource.Token);

                //task = Task.Run<int>(new Func<int>(MultiThreadMethod), token);
                //int result = await task; // スレッドの処理の結果を「待ち受け」する
            }
            catch (TaskCanceledException ex)
            {
                // キャンセルされた場合の例外処理
                Dbg.Log("RunMultiThread キャンセル：" + ex.ToString());
            }
            catch (Exception ex)
            {
                // 異常終了した場合の例外処理
                Dbg.Log("RunMultiThread エラー：" + ex.ToString());
            }

        }

        public virtual void MultiThreadCancel()
        {
            if(_tokenSource != null)
            {
                _tokenSource.Cancel();
                _tokenSource = null;
            } 

            Cancel = true;
        }

    }
}
