using okoshko.Utils;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace okoshko
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _filePath;
        private RangerAction _action;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Choose_File(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*|SSR files (*.ssr)|*.ssr";
            if (dialog.ShowDialog() == true)
            {
                _filePath = dialog.FileName;
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void DoAnAction(object sender, RoutedEventArgs e)
        {
            TextRangingUtil.RangeText(_filePath, _action);

        }

        private void Decode_RB_Checked(object sender, RoutedEventArgs e)
        {
            _action = RangerAction.Decode;
        }

        private void Encode_RB_Checked(object sender, RoutedEventArgs e)
        {
            _action = RangerAction.Encode;
        }
    }
    public enum RangerAction
    {
        Decode,
        Encode
    }
}