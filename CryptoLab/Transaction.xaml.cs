namespace CryptoLab
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    public partial class TransactionWindow : Window
    {
        public TransactionWindow()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var item = recipientComboBox.SelectedValue as ComboBoxItem;
            var recipient = item.Content.ToString();
            var amount = textBox2.Text;
            if (string.IsNullOrEmpty(recipient) && string.IsNullOrEmpty(amount))
            {
                MessageBox.Show("Receiver and total empty", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            var currentApp = Application.Current as App;
            var currentNodeId = currentApp.CurrentNodeId;
            AddTransaction(currentNodeId, int.Parse(recipient), int.Parse(amount));

            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddTransaction(int senderId, int recipientId, int amount)
        {
            var currentApp = Application.Current as App;
            currentApp.CryptoCore.CreateTransaction(recipientId, amount);

            var mainWindow = Owner as MainWindow;
            _ = mainWindow.TransactionsData.Items.Add(new
            {
                Sender = senderId,
                Recipient = recipientId,
                Amount = amount,
                Date = DateTime.Now.ToString("HH:mm:ss")
            });

            //mainWindow.UpdateBalance();
        }
    }
}
