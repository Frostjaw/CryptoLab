namespace CryptoLab
{
    using System.Windows;
    using System.Windows.Controls;

    //MainWindow Form = Application.Current.Windows[0] as MainWindow;
    public partial class TransactionWindow : Window
    {
        public TransactionWindow()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox textBoxRec = textBox1;
            TextBox textBoxTotal = textBox2;
            if (textBoxRec.GetLineText(0) != "" & textBoxTotal.GetLineText(0) != "")
            {
                AddTransactions(textBoxRec.GetLineText(0), "192.168.0.1", textBoxTotal.GetLineText(0));
                
                Close();
            }
            else
            {
                MessageBox.Show("Receiver and total empty", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddTransactions(string Receiver,string Sender, string total)
        {
            MainWindow mainWindow = Owner as MainWindow;
            //mainWindow.UpdateBalance();
            //mainWindow.TransactionsData.Items.Add(new Transactions { Receiver = Receiver, Sender = Sender, Total = total, Data = DateTime.Now.ToString("HH:mm:ss") });
        }
    }

    public class Transactions
    {
        public string Receiver { get; set; }
        public string Sender { get; set; }
        public string Total { get; set; }
        public string Data { get; set; }
    }
}
