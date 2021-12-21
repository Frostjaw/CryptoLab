using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Globalization;

namespace CryptoLab
{

    //MainWindow Form = Application.Current.Windows[0] as MainWindow;
    public partial class Transaction : Window
    {
        public Transaction()
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
                
                this.Close();
            }
            else
                MessageBox.Show("Receiver and total empty", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddTransactions(string Receiver,string Sender, string total)
        {
            MainWindow mainWindow = Owner as MainWindow;
            mainWindow.Update_Ballance(Double.Parse(total));
            mainWindow.TransactionsData.Items.Add(new Transactions { Receiver = Receiver, Sender = Sender, Total = total, Data = DateTime.Now.ToString("HH:mm:ss") });
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
