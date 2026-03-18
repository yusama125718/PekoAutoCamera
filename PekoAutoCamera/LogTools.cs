using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Shapes;

namespace PekoAutoCamera
{
    internal class LogTools
    {
        private float ball_x;
        private float ball_y;
        private float ball_z;
        private bool blue_low;
        private bool blue_high;
        private bool red_low;
        private bool red_high;

        private String logpath;
        public LogTools(String path) {
            logpath = path;
            ball_x = 0F;
            ball_y = 0F;
            ball_z = 0F;
            blue_low = true;
            blue_high = true;
            red_low = true;
            red_high = true;
        }

        // 解析実行
        public void Execute()
        {
            int BUFFER_SIZE = 32; // バッファーサイズ(あえて小さく設定)
            int lineCountToWrite = 30; // 探索行数
            var buffer = new byte[BUFFER_SIZE];
            var foundCount = 0;

            using (var fs = new FileStream(logpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // 検索ブロック位置の繰り返し
                for (var i = 0; ; i++)
                {
                    if (fs.Length <= i * BUFFER_SIZE)
                    {
                        // ファイルの先頭まで達した場合
                        Console.WriteLine("NOT FOUND");
                        string log = "";

                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            log = sr.ReadToEnd();
                        }
                        return;
                    }

                    // ブロック開始位置に移動
                    var offset = Math.Min((int)fs.Length, (i + 1) * BUFFER_SIZE);
                    fs.Seek(-offset, SeekOrigin.End);

                    // ブロックの読み込み
                    var readLength = offset - BUFFER_SIZE * i;
                    for (var j = 0; j < readLength; j += fs.Read(buffer, j, readLength - j)) ;

                    // ブロック内の改行コードの検索
                    for (var k = 0; k < readLength; k++)
                    {
                        if (buffer[k] == 0x0A)
                        {
                            var sr = new System.IO.StreamReader(fs, Encoding.UTF8);
                            fs.Seek(k + 3, SeekOrigin.Current);
                            string line = sr.ReadLine();
                            if (line != null && line.Contains("PEKO_INFO:"))
                            {
                                // PEKO_INFO:以降のみを解析する
                                string key = "PEKO_INFO:";
                                line = line.Substring(line.IndexOf(key) + key.Length);
                                // PEKO PEKO BATTLEのログだった場合
                                AnalyzeLog(line);
                            }
                            foundCount++;
                            if (foundCount == lineCountToWrite)
                            {
                                // 所定の行数を解析した場合
                                return;
                            }
                        }
                    }
                }
            }
        }

        // ログ解析
        private void AnalyzeLog(string line)
        {
            // ログの種類を取得
            string type = "";
            int start = line.IndexOf('<');
            int end = line.IndexOf('>');
            if (start >= 0 && end > start)
            {
                type = line.Substring(start + 1, end - start - 1);
            }

            // ログの種類に応じて処理
            switch (type)
            {
                case "track":
                    if (!line.Contains("object=ONIGIRI")) break;

                    // pos=内を抽出
                    string key = "pos=(";
                    string pos_str = line.Substring(line.IndexOf(key) + key.Length);
                    int pos_end = pos_str.IndexOf(')');
                    if (pos_end > 0) pos_str = pos_str.Substring(0, pos_end);

                    // 座標を抽出
                    string[] positions = pos_str.Split(", ");
                    ball_x = float.Parse(positions[0]);
                    ball_y = float.Parse(positions[1]);
                    ball_z = float.Parse(positions[2]);
                    break;

                case "event":
                    if (!line.Contains("type=HIT_COMMIT"))
                    {
                        // target=内を抽出
                        string target_key = "target=";
                        string target_str = line.Substring(line.IndexOf(target_key) + target_key.Length);
                        int target_end = target_str.IndexOf(' ');
                        if (target_end > 0) target_str = target_str.Substring(0, target_end);
                        Debug.WriteLine(target_str);

                        switch (target_str)
                        {
                            case "ORANGE_HIGH":
                                red_high = false;
                                break;

                            case "ORANGE_LOW":
                                red_low = false;
                                break;

                            case "BLUE_HIGH":
                                blue_high = false;
                                break;

                            case "BLUE_LOW":
                                blue_low = false;
                                break;

                            case "RED_GIRL":
                            case "BLUE_GIRL":
                                red_high = true;
                                red_low = true;
                                blue_high = true;
                                blue_low = true;
                                break;
                        }
                    }
                    break;
            }
        }

        public float GetBallX()
        {
            return ball_x;
        }

        public float GetBallY()
        {
            return ball_y;
        }

        public float GetBallZ()
        {
            return ball_z;
        }

        public bool GetRedHigh()
        {
            return red_high;
        }

        public bool GetRedLow()
        {
            return red_low;
        }

        public bool GetBlueHigh()
        {
            return blue_high;
        }

        public bool GetBlueLow()
        {
            return blue_low;
        }
    }
}
