using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertDaiwaForBPF
{
    internal class ConverterMain : BaseThread
    {
        private Dictionary<string, DataTable> mMasterSheets = null;

        public ConverterMain()
        {
        }

        private void ReadMasterFile()
        {
            if (mMasterSheets != null)
            {
                //2重読み込み防止
                return;
            }

            //Dbg.Log("master.xlsx 読み込み中...");

            UtilExcel excel = new UtilExcel();

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
            Dbg.Log("master.xlsx 読み込み終了");

            //DataTable sheet = mMasterSheets["項目マッピング"];

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

        public override void MultiThreadCancel()
        {
            Dbg.Log("変換キャンセル");

            if (csvHDR != null)
            {
                csvHDR.Cancel();
            }

            if (csvDTL != null)
            {
                csvDTL.Cancel();
            }

            base.MultiThreadCancel();
        }


        string mPathInput;
        string mPathHR;
        string mPathOutput;

        private UtilCsv csvHDR = null;
        private UtilCsv csvDTL = null;

        public void InitConvert(string pathInput, string pathHR, string pathOutput)
        {
            mPathInput = pathInput;
            mPathHR = pathHR;
            mPathOutput = pathOutput;

            csvHDR = new UtilCsv();
            csvDTL = new UtilCsv();

            Cancel = false;
            state = 0;
        }

        //スレッド内の処理（これ自体をキャンセルはできない）
        int state = -1;

        public override int MultiThreadMethod()
        {
            Dbg.Log("変換中...");
            DataTable hdr = null;
            DataTable tdl = null;

            bool loop = true;

            while (loop)
            {
                if(Cancel)
                {
                    return 0;
                }

                switch (state)
                { 
                    case 0:
                        {
                            ReadMasterFile();
                            state = 1;
                        }
                        break;

                    case 1:
                        {
                            hdr = csvHDR.ReadFile(mPathInput + "\\DHPTV001HED_458BNF.csv", ",", GlobalVariables.ENCORDTYE.UTF8);
                            if (hdr == null)
                            {
                                return 0;

                            }

                            if (hdr.Rows.Count == 0)
                            {
                                return 0;
                            }

                            state = 2;
                        }
                        break;

                    case 2:
                        {
                            SetColumnName(hdr, mMasterSheets["DHPTV001HED"]);
                            state = 3;
                        }
                        break;

                    case 3:
                        {
                            tdl = csvDTL.ReadFile(mPathInput + "\\DHPTV001DTL_458BNF.csv", ",", GlobalVariables.ENCORDTYE.UTF8);
                            if (tdl == null)
                            {
                                return 0;
                            }

                            if (tdl.Rows.Count == 0)
                            {
                                return 0;
                            }

                            state = 4;
                        }
                        break;

                    case 4:
                        {

                            SetColumnName(tdl, mMasterSheets["DHPTV001DTL"]);
                            state = 5;
                        }
                        break;


                    case 5:
                        {
                            //結合して取得
                            var query =
                                 from h in hdr.AsEnumerable()
                                 join d in tdl.AsEnumerable() on h.Field<string>("組合C") equals d.Field<string>("組合C")
                                 where 
                                    h.Field<string>("健診基本情報管理番号") == d.Field<string>("健診基本情報管理番号")
                                    && h.Field<string>("削除フラグ")  == "0"
                                    && d.Field<string>("削除フラグ")  == "0"
                                    && d.Field<string>("未実施FLG")   == "0"
                                    && d.Field<string>("測定不能FLG") == "0"
                                 select new
                                 {
                                     PersonNo = h.Field<string>("個人番号"),
                                     KenshinDate = h.Field<string>("健診実施日"),
                                     KenshinkikanName = h.Field<string>("健診実施機関名称"),
                                     KensakoumokuCode = d.Field<string>("検査項目コード"),
                                     KensakoumokuName = d.Field<string>("検査項目名称"),
                                     KenshinmeisaiNo = d.Field<string>("健診明細情報管理番号"),
                                     Value = d.Field<string>("結果値"),
                                     Comment = d.Field<string>("コメント"),
                                 };
                            /*
                            foreach (var item in query)
                            {
                                Dbg.Log(item.KensakoumokuCode + " "+ item.value);
                            }
                            */

                            //TODO:検査項目コードの置換

                            //TODO:項目マッピング

                            //TODO:コードマッピング

                            //TODO:ロジックマッピング

                            //TODO:オーダーマッピング

                            state = 6;
                        }
                        break;

                    //終了
                    default:
                        {
                            Dbg.Log("state:"+ state);
                            loop = false;
                            break;
                        }
                }
            }

            /*
            DataRow[] rows =
            tdl.AsEnumerable()
                .Where(x => x["組合C"].ToString() != "")
                .ToArray();

            foreach (DataRow row in rows)
                Dbg.Log(row["組合C"].ToString());
            */

            //処理完了
            Completed = true;
            return 1;
        }

        void SetColumnName(DataTable dt, DataTable sheet)
        {
            DataRow[] rows =
            sheet.AsEnumerable()
                .Where(x => x["項目"].ToString() != "")
                .ToArray();

            int n = dt.Columns.Count;

            for (int i=0; i< rows.Count(); i++)
            {
                //Dbg.Log(rows[i][0].ToString());

                if (Cancel)
                {
                   return;
                }

                if(i<n)
                {
                    dt.Columns[""+(i+1)].ColumnName = rows[i][0].ToString();
                }
                else
                {
                    dt.Columns.Add(rows[i][0].ToString());
                }
            }
        }

    }
}
