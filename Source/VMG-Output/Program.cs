using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VMG_Output
{
    static class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("VMGファイル変換出力ツール");
                Console.WriteLine("　指定されたフォルダにあるVMGファイルをマークダウン形式で出力します。");
                Console.WriteLine("　　実行方法：VMG-Output.exe [フォルダパス] [マークダウンファイル出力パス]");
                Environment.Exit(0);
            }
            // フォルダ存在チェック
            else if (!System.IO.Directory.Exists(args[0]))
            {
                Console.WriteLine(args[0] + " フォルダ読み込みエラー");
                Environment.Exit(1);
            }
            // 引数確認
            if(args.Length < 2)
            {
                Console.WriteLine(args[0] + " マークダウンファイル出力パスがありません");
                Environment.Exit(1);
            }

            // ファイル再帰読み込み
            var vmgList = new SortedDictionary<string, vmg>();
            var di = new System.IO.DirectoryInfo(args[0]);
            var vmg_files = di.EnumerateFiles("*.VMG", System.IO.SearchOption.AllDirectories);
            Console.WriteLine(vmg_files.Count() + " ファイルが見つかりました");
            int count_ok = 0;
            int count_sk = 0;
            int count_ng = 0;
            //ファイル列挙
            foreach (var f in vmg_files)
            {
                string text = String.Empty;
                // ファイル読込み
                using (var sr = new StreamReader(f.FullName, Encoding.GetEncoding(@"Shift_JIS")))
                {
                    text = sr.ReadToEnd();
                }
                // テキスト分解
                var mail = new vmg();
                // 先頭文字検索による要素読み込み
                foreach (string line in text.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                {
                    // 空
                    if (line.Length == 0)
                    {
                        continue;
                    }
                    // 日付
                    else if (line.StartsWith("Date: "))
                    {
                        string t = line.Remove(0, 6);

                        DateTimeOffset dto;

                        if (DateTimeOffset.TryParse(t, out dto))
                        {
                            mail.dt = dto.DateTime;
                        }
                        else
                        {
                            Console.WriteLine(f.FullName + " 日付取得失敗->" + line);
                            break;
                        }
                    }
                    // 送信元
                    else if (line.StartsWith("From: "))
                    {
                        string t = line.Remove(0, 6).Trim();
                        mail.From = t;
                    }
                    // 発信先
                    else if (line.StartsWith("To: "))
                    {
                        string t = line.Remove(0, 4).Trim();
                        mail.To = t;
                    }
                    // タイトル
                    else if (line.StartsWith("Subject: "))
                    {
                        string t = line.Remove(0, 9);
                        mail.subject = t;
                    }
                    // 他要素
                    else if (line.StartsWith("BEGIN:")
                        || line.StartsWith("END:")
                        || line.StartsWith("VERSION:")
                        || line.StartsWith("X-IRMC-")
                        || line.StartsWith("Cc: ")
                        || line.StartsWith("Bcc: ")
                        || line.StartsWith("MIME-Version: ")
                        || line.StartsWith("Content-")
                        || line.StartsWith("Reply-To: ")
                        || line.StartsWith("-------=_NextPart_")
                        )
                    {
                        // 無視
                        continue;
                    }
                    // 要素以外は本文扱い
                    else
                    {
                        mail.msg += line + "\r\n";
                    }
                }
                // キー生成
                if (mail.dt != null && mail.From != string.Empty && mail.To != string.Empty)
                {
                    string Key = string.Format("{0} {1} -> {2} [{3}]\r\n```\r\n{4}\r\n```\r\n",
                        mail.dt.ToString("yyyy/MM/dd HH:mm:ss"),
                        mail.From,
                        mail.To,
                        mail.subject,
                        mail.msg);
                    // 重複チェック
                    if (!vmgList.ContainsKey(Key))
                    {
                        vmgList.Add(Key, mail);
                        Console.WriteLine(f.FullName + " 読込[OK]");
                        count_ok++;
                    }
                    else
                    {
                        Console.WriteLine(f.FullName + " 重複[SKIP]");
                        count_sk++;
                    }
                }
                else
                {
                    Console.WriteLine(f.FullName + " キー無し除外[ERR]");
                    count_ng++;
                }
            }
            // 時系列合成
            var sb = new StringBuilder();
            foreach (var mai in vmgList)
            {
                sb.Append(mai.Key);
            }
            // 改行重複消去
            sb = sb.Replace("\r\n\r\n", "\r\n");

            try
            {
                // ファイル出力
                File.WriteAllText(args[1], sb.ToString());
                Console.WriteLine(string.Format("{0} 出力 (OK:{1}, SKIP:{2}, NG:{3}\r\n", args[1], count_ok, count_sk, count_ng));
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Environment.Exit(1);
            }
        }
    }

    public class vmg
    {
        public DateTime dt { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string subject { get; set; }
        public string msg { get; set; }
    }
}
