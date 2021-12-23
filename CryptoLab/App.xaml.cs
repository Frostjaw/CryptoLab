namespace CryptoLab
{
    using CryptoLab.Core;
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public CryptoCore CryptoCore;
        public int CurrentNodeId;
        public int Balance;

        void App_Startup(object sender, StartupEventArgs e)
        {
            CurrentNodeId = int.Parse(Environment.GetEnvironmentVariable("NodeId"));
            CryptoCore = new CryptoCore(CurrentNodeId);
            CryptoCore.InitializeBlockChain();

            bool startMinimized = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/StartMinimized")
                {
                    startMinimized = true;
                }
            }

            MainWindow mainWindow = new MainWindow();
            if (startMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
            }
            mainWindow.Show();
        }
    }
}