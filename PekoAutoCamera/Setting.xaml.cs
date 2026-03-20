using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Setting.xaml の相互作用ロジック
    /// </summary>
    public partial class Setting : Page
    {
        Regex number_regex = new Regex("[^0-9]+");


        public Setting()
        {
            InitializeComponent();

            // ログファイルの初期値を設定
            string locallow = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "Low/VRChat/VRChat";
            string[] texts = Directory.GetFiles(locallow, "*txt");
            string log_file = "";
            foreach (string s in texts)
            {
                if (s.Contains("output_log") && String.Compare(s, log_file) == 1)
                {
                    log_file = s;
                }
            }
            logpath_txt.Text = log_file;

            // OSC接続情報の初期値を設定
            osc_address.Text = "127.0.0.1";
            osc_port.Text = "9000";
        }

        // 開始ボタン
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // ログパスの存在確認
            if (!File.Exists(logpath_txt.Text))
            {
                MessageBox.Show("ログパスのファイルが存在しません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // ポート番号の確認
            if (number_regex.IsMatch(osc_port.Text))
            {
                MessageBox.Show("ポート番号は数字で入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int port = int.Parse(osc_port.Text);
            if (port < 0 || 25535 < port)
            {
                MessageBox.Show("ポート番号は0から25535の範囲で入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 画面切り替え
            CameraProgress content = new CameraProgress(logpath_txt.Text, osc_address.Text, port);
            NavigationService.Navigate(content);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        // ポート番号を数字のみにする
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var text = ((TextBox)sender).Text + e.Text;
            e.Handled = number_regex.IsMatch(text);
        }
    }
}
