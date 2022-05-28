﻿using System;
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
        public bool Cancel { get; set; }            //キャンセルフラグ
        public bool Completed { get; set; }         //完了フラグ

        /// <summary>
        /// キャンセル用トークン
        /// </summary>
        private CancellationTokenSource _tokenSource = null;

        //コンストラクタ
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
            //変数初期化
            Cancel = false;
            Completed = false;

             // キャンセルトークンソースを生成し、キャンセルトークンを取得します。
            if (_tokenSource == null) 
                _tokenSource = new CancellationTokenSource();

            //非同期処理（マルチスレッド）開始
            try
            {
                //Dbg.Log("RunMultiThread");

                var task = Task.Factory.StartNew(() =>
                {
                    if(MultiThreadMethod() == 0)
                    {
                        Cancel=true;
                    }
                }, _tokenSource.Token);

                //task = Task.Run<int>(new Func<int>(MultiThreadMethod), token);
                //int result = await task; // スレッドの処理の結果を「待ち受け」する
            }
            catch (TaskCanceledException ex)
            {
                // キャンセルされた場合の例外処理
                Dbg.Debug("RunMultiThread キャンセル：" + ex.ToString());
            }
            catch (Exception ex)
            {
                // 異常終了した場合の例外処理
                Dbg.ErrorWithView("RunMultiThread エラー：" + ex.ToString());

                throw ex;
            }

        }

        /// <summary>
        /// スレッドのキャンセル
        /// </summary>
        public virtual void MultiThreadCancel()
        {
            Dbg.ViewLog("変換キャンセル");
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
                _tokenSource = null;
            } 

            Cancel = true;
        }

    }
}
