using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rug.Osc;

namespace PekoAutoCamera
{
    /// <summary>
    /// CameraProgress.xaml の相互作用ロジック
    /// </summary>
    public partial class CameraProgress : Page
    {
        private float ball_x;
        private float ball_y;
        private float ball_z;
        private bool blue_low;
        private bool blue_high;
        private bool red_low;
        private bool red_high;

        private bool cam1_enabled = false;
        private bool cam2_enabled = false;
        // 2カメの映しているオブジェクト
        // 1 : 青側低台
        // 2 : 青側高台
        // 3 : 赤側低台
        // 4 : 赤側高台
        // 5 : 青側ゴール
        // 6 : 赤側ゴール
        private int cam2_position = 0;

        private String logpath;
        private String oscAddress;
        private int oscPort;
        private CancellationTokenSource _cts;
        private UdpClient udpClient;
        private List<OscMessage> message_cache = new List<OscMessage>();
        private LogTools logtool;
        private Dictionary<int, (double x, double y)> crystals_mst = new Dictionary<int, (double x, double y)>
        {
            { 1, (10.0, 20.0) },
            { 2, (30.5, 40.2) },
            { 3, (5.0, 8.0) },
            { 4, (10.0, 20.0) },
            { 5, (30.5, 40.2) },
            { 6, (5.0, 8.0) }
        };

        public CameraProgress(String log, String address, int port)
        {
            InitializeComponent();

            // 各種変数初期化
            logpath = log;
            _cts = new CancellationTokenSource();
            oscPort = port;
            oscAddress = address;
            udpClient = new UdpClient(oscAddress, oscPort);
            logtool = new LogTools(logpath);

            // ボタン初期化
            ChangeButtonState(cam1_btn, cam1_enabled);

            // 処理開始
            LoopMethod();
        }

        // メイン処理
        private async void LoopMethod()
        {
            
            logtool.Execute();
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    // プロパティの値を取得
                    ball_x = logtool.GetBallX();
                    ball_y = logtool.GetBallY();
                    ball_z = logtool.GetBallZ();
                    blue_high = logtool.GetBlueHigh();
                    blue_low = logtool.GetBlueLow();
                    red_high = logtool.GetRedHigh();
                    red_low = logtool.GetRedLow();

                    // 情報タブ更新
                    ball_x_text.Text = ball_x.ToString();
                    ball_y_text.Text = ball_y.ToString();
                    ball_z_text.Text = ball_z.ToString();
                    if (blue_low) blue_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                    else blue_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                    if (blue_high) blue_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                    else blue_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                    if (red_low) red_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                    else red_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                    if (red_high) red_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                    else red_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));

                    // マップ更新
                    OrigiriTranslate.X = ball_z * 4.4;
                    OrigiriTranslate.Y = ball_x * 4.4;

                    // カメラ座標設定
                    UpdateCamera();

                    await Task.Delay(100);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void UpdateCamera()
        {
            List<OscMessage> messages = new List<OscMessage>();

            if (cam1_enabled)
            {
                // 1カメの位置、角度を決定（サイドライン側の固定カメラ）
                double cam1_x = 25;
                double cam1_y = 10;
                double cam1_z = 0;
                double diff1_x = (double) ball_x - cam1_x;
                double diff1_y = (double) ball_y - cam1_y;
                double diff1_z = (double) ball_z - cam1_z;
                double horizontal_distance1 = Math.Sqrt(Math.Pow(diff1_x, 2) + Math.Pow(diff1_z, 2));
                double distance1_3D = Math.Sqrt(Math.Pow(diff1_x, 2) + Math.Pow(diff1_y, 2) + Math.Pow(diff1_z, 2));
                double cam1_h = Math.Atan2(diff1_x, diff1_z) * 180 / Math.PI;
                double cam1_v = Math.Atan2(diff1_y, horizontal_distance1) * 180 / Math.PI * 0.8;
                double cam1_fov = 60 - distance1_3D * 1;

                // OSCの内容に追加
                messages.Add(new OscMessage("/avatar/parameters/01PosX", new object[] { (float)_105BankConverter.ConvertX(cam1_x) }));
                messages.Add(new OscMessage("/avatar/parameters/01PosY", new object[] { (float)_105BankConverter.ConvertY(cam1_y) }));
                messages.Add(new OscMessage("/avatar/parameters/01PosZ", new object[] { (float)_105BankConverter.ConvertZ(cam1_z) }));
                messages.Add(new OscMessage("/avatar/parameters/01RotH", new object[] { (float)_105BankConverter.ConvertHorizontal(cam1_h) }));
                messages.Add(new OscMessage("/avatar/parameters/01RotV", new object[] { (float)_105BankConverter.ConvertVertical(cam1_v) }));
                messages.Add(new OscMessage("/avatar/parameters/01FoV", new object[] { (float)_105BankConverter.ConvertFov(cam1_fov) }));
            }

            if (cam2_enabled && !logtool.GetBreakFlg())
            {
                // 2カメの位置、角度を決定（最寄りクリスタルへの固定カメラ）
                double cam2_x = 0;
                double cam2_y = 8;
                double cam2_z = 0;
            }

            if (message_cache.Count != 0)
            {
                foreach (OscMessage m in message_cache) messages.Add(m);
                message_cache.Clear();
            }
            // メッセージがあれば送信
            if (messages.Count != 0)
            {
                // OSC送信
                udpClient.Send(new OscBundle(new OscTimeTag(), messages.ToArray()).ToByteArray());
            }
            
        }

        private void ChangeButtonState(Button target, bool state)
        {
            if (state) target.Background = new SolidColorBrush(Color.FromArgb(255, 150, 255, 150));
            else target.Background = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160));
        }

        // カメラ1ボタン
        private void Cam1Button_Click(object sender, RoutedEventArgs e)
        {
            cam1_enabled = !cam1_enabled;
            ChangeButtonState(cam1_btn, cam1_enabled);
        }

        // カメラ2ボタン
        private void Cam2Button_Click(object sender, RoutedEventArgs e)
        {
            cam2_enabled = !cam2_enabled;
            ChangeButtonState(cam2_btn, cam2_enabled);
        }

        // システム起動ボタン
        private void SystemOnButton_Click(object sender, RoutedEventArgs e)
        {
            // OSCのキャッシュに追加
            message_cache.Add(new OscMessage("/avatar/parameters/IsSystemEnabled", new object[] { true }));
            MessageBox.Show("送信しました");
        }

        // システム終了ボタン
        private void SystemOffButton_Click(object sender, RoutedEventArgs e)
        {
            // 確認ダイアログを表示する
            if (MessageBox.Show("終了メッセージを送信しますか",
                    "Save file",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // OSCのキャッシュに追加
                message_cache.Add(new OscMessage("/avatar/parameters/IsSystemEnabled", new object[] { false }));
                MessageBox.Show("送信しました");
            }
        }

        // 終了ボタン
        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            // タイトルに戻ります。よろしいでしょうか？
            if (MessageBox.Show("終了メッセージを送信しますか",
                    "Save file",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // ループ処理終了
                _cts?.Cancel();
                // 画面切り替え
                Setting content = new Setting();
                NavigationService.Navigate(content);
            }
        }
    }
}
