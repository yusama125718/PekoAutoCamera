using System;
using System.Collections.Generic;
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
    /// Setting.xaml の相互作用ロジック
    /// </summary>
    public partial class Setting : Page
    {
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

            // 画面切り替え
            CameraProgress content = new CameraProgress(logpath_txt.Text);
            NavigationService.Navigate(content);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
