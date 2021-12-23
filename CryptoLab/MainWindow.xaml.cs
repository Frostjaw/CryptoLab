namespace CryptoLab
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var currentApp = Application.Current as App;
            var balance = currentApp.CryptoCore.GetBalance();
            BalanceText.Content = balance.ToString();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            TransactionWindow TranWindow = new TransactionWindow();
            TranWindow.Owner = this;
            TranWindow.Show();
        }

        public void UpdateBalance()
        {
            var currentApp = Application.Current as App;
            var balance = currentApp.CryptoCore.GetBalance();
            BalanceText.Content = balance.ToString();

            /*double ball_old = double.Parse(this.BallanceText.Content.ToString());
            ball_old += ball;
            BallanceText.Content =ball_old.ToString();*/
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
