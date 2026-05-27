using System.Windows;
using okoshko.Services;
using okoshko.ViewModels;

namespace okoshko;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainWindowViewModel(
            new FileDialogService(),
            new TextRangingService());
    }
}