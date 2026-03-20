using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

        private String logpath;
        private String oscAddress;
        private int oscPort;
        private CancellationTokenSource _cts;
        UdpClient udpClient;

        public CameraProgress(String log, String address, int port)
        {
            InitializeComponent();
            logpath = log;
            _cts = new CancellationTokenSource();
            oscPort = port;
            oscAddress = address;
            udpClient = new UdpClient(oscAddress, oscPort);
            LoopMethod();
        }

        // メイン処理
        private async void LoopMethod()
        {
            LogTools logtool = new LogTools(logpath);
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
            double cam1_x = 25;
            double cam1_y = 10;
            double cam1_z = 0;
            double diff1_x = (double) ball_x - cam1_x;
            double diff1_y = (double) ball_y - cam1_y;
            double diff1_z = (double) ball_z - cam1_z;
            double horizontal_distance1 = Math.Sqrt(Math.Pow(diff1_x, 2) + Math.Pow(diff1_z, 2));
            double distance1_3D = Math.Sqrt(Math.Pow(diff1_x, 2) + Math.Pow(diff_y, 2) + Math.Pow(diff1_z, 2));
            double cam1_h = Math.Atan2(diff1_x, diff1_z) * 180 / Math.PI;
            double cam1_v = Math.Atan2(diff1_y, horizontal_distance1) * 180 / Math.PI * 0.8;
            double cam1_fov = 60 - distance1_3D * 1;

            OscBundle bundle = new OscBundle(new OscTimeTag(), new OscMessage[]
            {
                new OscMessage("/avatar/parameters/01PosX", new object[] { (float) _105BankConverter.ConvertX(cam1_x) }),
                new OscMessage("/avatar/parameters/01PosY", new object[] { (float) _105BankConverter.ConvertY(cam1_y) }),
                new OscMessage("/avatar/parameters/01PosZ", new object[] { (float) _105BankConverter.ConvertZ(cam1_z) }),
                new OscMessage("/avatar/parameters/01RotH", new object[] { (float) _105BankConverter.ConvertHorizontal(cam1_h) }),
                new OscMessage("/avatar/parameters/01RotV", new object[] { (float) _105BankConverter.ConvertVertical(cam1_v) }),
                new OscMessage("/avatar/parameters/01FoV", new object[] { (float) _105BankConverter.ConvertFov(cam1_fov) })
            });
            udpClient.Send(bundle.ToByteArray());
        }

        private void UnloadHandler(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        public void SetLogpath(String path)
        {
            logpath = path;
        }

        public String GetLogpath()
        {
            return logpath;
        }
    }
}
