using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace PekoAutoCamera
{
    /// <summary>
    /// CameraProgress.xaml の相互作用ロジック
    /// </summary>
    public partial class CameraProgress : Page
    {
        private String logpath = "";
        private CancellationTokenSource _cts;


        public CameraProgress(String log)
        {
            InitializeComponent();
            logpath = log;
            _cts = new CancellationTokenSource();
            LoopMethod();
        }


        private async void LoopMethod()
        {
            LogTools logtool = new LogTools(logpath);
            logtool.Execute();
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    ball_x.Text = logtool.GetBallX().ToString();
                    ball_y.Text = logtool.GetBallY().ToString();
                    ball_z.Text = logtool.GetBallZ().ToString();

                    if (logtool.GetBlueLow()) blue_low.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                    else blue_low.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                    if (logtool.GetBlueHigh()) blue_high.Background = new SolidColorBrush(Color.FromArgb(255, 175, 175, 255));
                    else blue_high.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                    if (logtool.GetRedLow()) red_low.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                    else red_low.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                    if (logtool.GetRedHigh()) red_high.Background = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
                    else red_high.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));

                    await Task.Delay(200);
                }
            }
            catch (TaskCanceledException)
            {
            }
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
