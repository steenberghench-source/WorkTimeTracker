using System.ComponentModel;
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
using WorkTimeTracker.Storage;
using WorkTimeTracker.ViewModels;

namespace WorkTimeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new WeekViewModel();

            this.Closing += MainWindow_Closing;
        }
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is WeekViewModel vm)
            {
                WeekRepository.Save(vm.HuidigeWeekStart, vm.Dagen, vm.ReedsAfgedrukt);
            }
        }
    }
}