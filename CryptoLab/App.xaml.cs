namespace CryptoLab
{
    using CryptoLab.Core;
    using CryptoLab.Core.Models;
    using CryptoLab.NetworkModule;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string NodesInfoPath = "nodesInfo.json";

        public CryptoCore CryptoCore;
        public int CurrentNodeId;
        public int Balance;

        private List<NodeInfo> _nodesInfo;

        void App_Startup(object sender, StartupEventArgs e)
        {
            _nodesInfo = JsonConvert.DeserializeObject<List<NodeInfo>>(File.ReadAllText(NodesInfoPath));

            CurrentNodeId = int.Parse(Environment.GetEnvironmentVariable("NodeId"));

            CryptoCore = new CryptoCore(CurrentNodeId);
            CryptoCore.InitializeBlockChain();

            var currentNodePort = _nodesInfo.Find(ni => ni.Id == CurrentNodeId).Port;



/*            var transaction = new Transaction
            {
                Inputs = new TransactionInput[] { },
                Outputs = new TransactionOutput[]
    {
                    new TransactionOutput
                    {
                        Value = 10,
                        ScriptPublicKey = "qwe",
                    },
                    new TransactionOutput
                    {
                        Value = 10,
                        ScriptPublicKey = "asd",
                    },
                    new TransactionOutput
                    {
                        Value = 10,
                        ScriptPublicKey = "zxc",
                    },
                    new TransactionOutput
                    {
                        Value = 10,
                        ScriptPublicKey = "asdf",
                    },
                    new TransactionOutput
                    {
                        Value = 10,
                        ScriptPublicKey = "bnasd",
                    }
    }
            };

            var transactionJson = JsonConvert.SerializeObject(transaction);
            var deserializedTransaction = JsonConvert.DeserializeObject<Transaction>(transactionJson);
            var transactionBytes = Encoding.UTF8.GetBytes(transactionJson);

            webModuleHost.sendTransaction(transactionBytes);

            Thread.Sleep(2000);
            webModuleHost.sendTransaction(transactionBytes);*/

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