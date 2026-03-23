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
        private bool break_flg;

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
        private Dictionary<int, (double x, double z)> active_crystals = new Dictionary<int, (double x, double z)>();

        private String logpath;
        private String oscAddress;
        private int oscPort;
        private CancellationTokenSource _cts;
        private UdpClient udpClient;
        private List<OscMessage> message_cache = new List<OscMessage>();
        private LogTools logtool;
        // クリスタルの座標マスタ
        private Dictionary<int, (double x, double z)> crystals_mst = new Dictionary<int, (double x, double z)>
        {
            { 1, (10.0, -20.0) },
            { 2, (-17.5, -23.5) },
            { 3, (-10.0, 20.0) },
            { 4, (17.5, 23.5) },
            { 5, (0, -32) },
            { 6, (0, 32) }
        };
        // 2カメ用クリスタルごとのアングルマスタ
        private Dictionary<int, (double x, double y, double z, double fov, double horizontal, double vertical)> crystals_pos_mst = new Dictionary<int, (double x, double y, double z, double fov, double horizontal, double vertical)>
        {
            { 1, (6.5, 6, -8.5, 50, 145, -25) },
            { 2, (-10, 8, -6.5, 60, 195, -25) },
            { 3, (-6.5, 6, 8.5, 50, 325, -25) },
            { 4, (10, 8, 6.5, 60, 15, -25) },
            { 5, (0, 4, -13, 60, 180, -20) },
            { 6, (0, 4, 13, 60, 0, -20) }
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
            red_high = true;
            red_low = true;
            blue_high = true;
            blue_low = true;
            active_crystals = new()
            {
                { 1, crystals_mst[1] },
                { 2, crystals_mst[2] },
                { 3, crystals_mst[3] },
                { 4, crystals_mst[4] },
            };
            red_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
            red_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
            blue_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
            blue_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));

            // ボタン初期化
            ChangeButtonState(cam1_btn, cam1_enabled);

            // イベント登録
            logtool.OnBlueLowChanged += async () => await OnBlueLowChanged();
            logtool.OnBlueHighChanged += async () => await OnBlueHighChanged();
            logtool.OnRedLowChanged += async () => await OnRedLowChangedAsync();
            logtool.OnRedHighChanged += async () => await OnRedHighChanged();
            logtool.OnGirlChanged += async () => await OnGirlChanged();

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

                    // 情報タブ更新
                    ball_x_text.Text = ball_x.ToString();
                    ball_y_text.Text = ball_y.ToString();
                    ball_z_text.Text = ball_z.ToString();

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

        private async Task OnBlueHighChanged()
        {
            blue_high = logtool.GetBlueHigh();
            if (blue_high && !active_crystals.ContainsKey(2)) active_crystals.Add(2, crystals_mst[2]);
            else if (active_crystals.ContainsKey(2)) active_crystals.Remove(2);
            if (!blue_high && !blue_low && !active_crystals.ContainsKey(5)) active_crystals.Add(5, crystals_mst[5]);
            else if ((blue_high || blue_low) && active_crystals.ContainsKey(5)) active_crystals.Remove(5);
            Dispatcher.Invoke(() =>
            {
                if (blue_high) blue_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                else blue_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            });
            await SetBreakFlg(3000);
        }

        private async Task OnBlueLowChanged()
        {
            if (blue_low && !active_crystals.ContainsKey(1)) active_crystals.Add(1, crystals_mst[1]);
            else if (active_crystals.ContainsKey(1)) active_crystals.Remove(1);
            blue_low = logtool.GetBlueLow();
            if (!blue_high && !blue_low && !active_crystals.ContainsKey(5)) active_crystals.Add(5, crystals_mst[5]);
            else if ((blue_high || blue_low) && active_crystals.ContainsKey(5)) active_crystals.Remove(5);
            Dispatcher.Invoke(() =>
            {
                if (blue_low) blue_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                else blue_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            });
            await SetBreakFlg(3000);
        }

        private async Task OnRedHighChanged()
        {
            if (red_high && !active_crystals.ContainsKey(4)) active_crystals.Add(4, crystals_mst[4]);
            else if (active_crystals.ContainsKey(4)) active_crystals.Remove(4);
            red_high = logtool.GetRedHigh();
            if (!red_high && !red_low && !active_crystals.ContainsKey(6)) active_crystals.Add(6, crystals_mst[6]);
            else if ((red_high || red_low) && active_crystals.ContainsKey(6)) active_crystals.Remove(6);
            Dispatcher.Invoke(() =>
            {
                if (red_high) red_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                else red_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            });
            await SetBreakFlg(3000);
        }

        private async Task OnRedLowChangedAsync()
        {
            if (red_low && !active_crystals.ContainsKey(3)) active_crystals.Add(3, crystals_mst[3]);
            else if (active_crystals.ContainsKey(3)) active_crystals.Remove(3);
            red_low = logtool.GetRedLow();
            if (!red_high && !red_low && !active_crystals.ContainsKey(6)) active_crystals.Add(6, crystals_mst[6]);
            else if ((red_high || red_low) && active_crystals.ContainsKey(6)) active_crystals.Remove(6);
            Dispatcher.Invoke(() =>
            {
                if (red_low) red_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                else red_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            });
            await SetBreakFlg(3000);
        }

        private async Task OnGirlChanged()
        {
            red_high = true;
            red_low = true;
            blue_high = true;
            blue_low = true;
            active_crystals = new()
            {
                { 1, crystals_mst[1] },
                { 2, crystals_mst[2] },
                { 3, crystals_mst[3] },
                { 4, crystals_mst[4] },
            };
            Dispatcher.Invoke(() =>
            {
                red_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                red_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                blue_high_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                blue_low_text.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
            });
            await SetBreakFlg(10000);
        }

        // クリスタル破壊フラグを一定時間保持する
        async Task SetBreakFlg(int time)
        {
            break_flg = true;
            await Task.Delay(time);
            break_flg = false;
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
                (double fov, double horizontal, double vertical) cam1_angle = GetAngle((ball_x, ball_y, ball_z), (cam1_x, cam1_y, cam1_z));

                // OSCの内容に追加
                messages.Add(new OscMessage("/avatar/parameters/01PosX", new object[] { (float)_105BankConverter.ConvertX(cam1_x) }));
                messages.Add(new OscMessage("/avatar/parameters/01PosY", new object[] { (float)_105BankConverter.ConvertY(cam1_y) }));
                messages.Add(new OscMessage("/avatar/parameters/01PosZ", new object[] { (float)_105BankConverter.ConvertZ(cam1_z) }));
                messages.Add(new OscMessage("/avatar/parameters/01RotH", new object[] { (float)_105BankConverter.ConvertHorizontal(cam1_angle.horizontal) }));
                messages.Add(new OscMessage("/avatar/parameters/01RotV", new object[] { (float)_105BankConverter.ConvertVertical(cam1_angle.vertical) }));
                messages.Add(new OscMessage("/avatar/parameters/01FoV", new object[] { (float)_105BankConverter.ConvertFov(cam1_angle.fov) }));
            }

            if (cam2_enabled && !break_flg && !( -1 > ball_x && ball_x > 1 && -1 > ball_z && ball_z > 1))
            {
                // 2カメの位置、角度を決定（最寄りクリスタルへの固定カメラ）
                // 一番近いクリスタルのカメラアングルを取得
                int cam2_position = active_crystals
                    .OrderBy(kv => DistanceSquared(kv.Value, ball_x, ball_z))
                    .First().Key;
                (double x, double y, double z, double fov, double horizontal, double vertical) cam2 = crystals_pos_mst[cam2_position];

                // 水平方向は補正を掛ける
                (double fov, double horizontal, double vertical) temp_angle = GetAngle((ball_x, ball_y, ball_z), (cam2.x, cam2.y, cam2.z)); 
                double to_ball_horizontal = temp_angle.horizontal % 360;
                if (to_ball_horizontal > 180) to_ball_horizontal -= 360; // -180から180の範囲にする
                if (to_ball_horizontal < -180) to_ball_horizontal += 360;
                double horizontal_tmp = cam2.horizontal % 360;
                if (horizontal_tmp > 180) horizontal_tmp -= 360;
                else if (horizontal_tmp < -180) horizontal_tmp += 360;
                double horizontal_diff = to_ball_horizontal - horizontal_tmp;
                double maxFollow = 50.0;               // 基準角度から最大30度までしか追わない
                double softness = 20.0;                // 大きいほどゆるやか
                double attenuatedDiff = maxFollow * Math.Tanh(horizontal_diff / softness);
                double fixed_horizontal = horizontal_tmp + attenuatedDiff;

                // OSCの内容に追加
                messages.Add(new OscMessage("/avatar/parameters/02PosX", new object[] { (float)_105BankConverter.ConvertX(cam2.x) }));
                messages.Add(new OscMessage("/avatar/parameters/02PosY", new object[] { (float)_105BankConverter.ConvertY(cam2.y) }));
                messages.Add(new OscMessage("/avatar/parameters/02PosZ", new object[] { (float)_105BankConverter.ConvertZ(cam2.z) }));
                messages.Add(new OscMessage("/avatar/parameters/02RotH", new object[] { (float)_105BankConverter.ConvertHorizontal(fixed_horizontal) }));
                messages.Add(new OscMessage("/avatar/parameters/02RotV", new object[] { (float)_105BankConverter.ConvertVertical(temp_angle.vertical - 10) }));
                messages.Add(new OscMessage("/avatar/parameters/02FoV", new object[] { (float)_105BankConverter.ConvertFov(cam2.fov) }));
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

        private (double fov, double horizontal, double vertical) GetAngle((double x, double y, double z) ball_pos, (double x, double y, double z) cam_pos)
        {
            double diff1_x = (double)ball_pos.x - cam_pos.x;
            double diff1_y = (double)ball_pos.y - cam_pos.y;
            double diff1_z = (double)ball_pos.z - cam_pos.z;
            double horizontal_distance = Math.Sqrt(Math.Pow(diff1_x, 2) + Math.Pow(diff1_z, 2));
            double distance_3D = Math.Sqrt(Math.Pow(diff1_x, 2) + Math.Pow(diff1_y, 2) + Math.Pow(diff1_z, 2));
            double h = Math.Atan2(diff1_x, diff1_z) * 180 / Math.PI;
            double v = Math.Atan2(diff1_y, horizontal_distance) * 180 / Math.PI * 0.8;
            double fov = 60 - distance_3D * 1;
            return (fov, h, v);
        }

        static double DistanceSquared((double x, double y) p, double x, double y)
        {
            double dx = p.x - x;
            double dy = p.y - y;
            return dx * dx + dy * dy;
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
