using System.Windows;

namespace FinanceApp.Views
{
    public partial class PopUpWindow : Window
    {
        public string Message { get; }

        public PopUpWindow(string message, string title)
        {
            InitializeComponent();
            Message = message;
            DataContext = this;
            Title = title;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
