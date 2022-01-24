namespace CryptoLab.Core
{
    using CryptoLab.Core.Models;
    using CryptoLab.NetworkModule;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    public class CryptoCore
    {
        private const string NodesInfoPath = "nodesInfo.json";
        private const byte PowDifficulty = 5;

        private readonly List<NodeInfo> _nodesInfo;
        private readonly string _currentNodeKeys;
        private readonly DbAccessor _dbAccessor;

        private readonly Host _host;

        private List<(Transaction, int)> _myUnspentTransactionOutputs;
        private List<(Transaction, int)> _allUnspentTransactionOutputs;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        public CryptoCore(int nodeId)
        {
            _nodesInfo = JsonConvert.DeserializeObject<List<NodeInfo>>(File.ReadAllText(NodesInfoPath));

            _currentNodeKeys = _nodesInfo.Find(ni => ni.Id == nodeId).RsaKey;
            _dbAccessor = new DbAccessor();

            _host = new Host(nodeId, _nodesInfo);
            _host.dataReceived += getDataByHeader;
            _host.startListeningInThread();

            _myUnspentTransactionOutputs = GetMyUnspentTransactionOutputs();
            _allUnspentTransactionOutputs = GetAllUnspentTransactionOutputs();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public void InitializeBlockChain()
        {
            // HERE
            _dbAccessor.ClearDb();

            var blocks = _dbAccessor.GetAllBlocks();
            if (blocks.Any())
            {
                return;
            }

            var genesisTransactions = new Transaction[]
            {
                new Transaction
                {
                    Inputs = new TransactionInput[] { },
                    Outputs = new TransactionOutput[]
                    {
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 1).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 2).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 3).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 4).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 5).RsaKey,
                        }
                    }
                }
            };

            var genesisBlock = new Block
            {
                PreviousBlockHeaderHash = new byte[] { },
                TransactionsHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(genesisTransactions)),
                // TODO POF
                ProofOfWorkCounter = 0,
                Transactions = genesisTransactions,
            };

            _dbAccessor.ClearDb();
            _dbAccessor.AddBlock(genesisBlock);

            // refresh unspent mine and all lists
            RefreshMyUnspentTransactionOutputsFromBlockChain();
            RefreshAllUnspentTransactionOutputsFromBlockChain();
        }

        // TODO call only when there is enough balance on current key (or write validation inside)
        public void CreateTransaction(int recipientId, int amount)
        {
            var recipientKey = _nodesInfo.Find(ni => ni.Id == recipientId).RsaKey;

            var (transactionToSpend, transactionToSpendOutputIndex) = FindTransactionToSpend(amount);

            var newTransaction = new Transaction
            {
                Inputs = new TransactionInput[]
                {
                    new TransactionInput
                    {
                        PreviousTransactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transactionToSpend)),
                        PreviousTransactionOutputIndex = transactionToSpendOutputIndex,
                    }
                },
                Outputs = new TransactionOutput[]
                {
                    new TransactionOutput
                    {
                        Value = amount,
                        ScriptPublicKey = recipientKey,
                    },
                    new TransactionOutput
                    {
                        Value = transactionToSpend.Outputs[transactionToSpendOutputIndex].Value - amount,
                        ScriptPublicKey = _currentNodeKeys,
                    }
                },
            };

            using var signer = new CustomSigner(_currentNodeKeys);
            var signature = signer.CreateSignature(newTransaction);

            foreach (var input in newTransaction.Inputs)
            {
                input.ScriptSignature = Convert.ToBase64String(signature) + " " + _currentNodeKeys;
            }

            // update local mine and all unspent transactions outputs
            //_myUnspentTransactionOutputs.Remove((transactionToSpend, transactionToSpendOutputIndex));
            //_allUnspentTransactionOutputs.Remove((transactionToSpend, transactionToSpendOutputIndex));

            // TODO send newTransaction to all nodes
            var transactionJson = JsonConvert.SerializeObject(newTransaction);
            var deserializedTransaction = JsonConvert.DeserializeObject<Transaction>(transactionJson);
            var transactionBytes = Encoding.UTF8.GetBytes(transactionJson);

            AddTransactionToPool(newTransaction);

            _host.sendTransaction(transactionBytes);
        }

        public void AddTransactionToPool(Transaction transaction)
        {
            if (!IsTransactionValid(transaction))
            {
                return;
            }

            var transactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));
            var existedTransaction = _dbAccessor.GetTransactionByHash(transactionHash);

            if (existedTransaction != null)
            {
                return;
            }

            // remove spent outputs from local hash
            foreach (var input in transaction.Inputs)
            {
                var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
                if (_myUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                {
                    _myUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                }

                if (_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                {
                    _allUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                }
            }

            // add new unspent outputs to local hash
            for (var i = 0; i < transaction.Outputs.Length; i++)
            {
                var output = transaction.Outputs[i];
                if (output.ScriptPublicKey == _currentNodeKeys)
                {
                    _myUnspentTransactionOutputs.Add((transaction, i));
                    _allUnspentTransactionOutputs.Add((transaction, i));

                    continue;
                }

                _allUnspentTransactionOutputs.Add((transaction, i));
            }

            _dbAccessor.AddTransactionToPool(transaction);

            var transactionPool = _dbAccessor.GetTransactionPool();
            
            // TODO move to const
            if (transactionPool.Count == 3)
            {
                CreateBlock();
            }            
        }

        public void AddBlock(Block block)
        {
            // refresh local cache
            foreach (var transaction in block.Transactions)
            {
                var transactionPool = _dbAccessor.GetTransactionPool();
                if (transactionPool.Contains(transaction))
                {
                    transactionPool.Remove(transaction);

                    continue;
                }

                // HERE new block doesn't delete anything from _myUnspentTransactionOutputs
                // remove spent outputs from local cache
                foreach (var input in transaction.Inputs)
                {
                    var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
                    if (_myUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                    {
                        _myUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                    }

                    if (_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                    {
                        _allUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                    }
                }

/*                // add new unspent outputs to local hash
                for (var i = 0; i < transaction.Outputs.Length; i++)
                {
                    var output = transaction.Outputs[i];
                    if (output.ScriptPublicKey == _currentNodeKeys)
                    {
                        _myUnspentTransactionOutputs.Add((transaction, i));
                        _allUnspentTransactionOutputs.Add((transaction, i));

                        continue;
                    }

                    _allUnspentTransactionOutputs.Add((transaction, i));
                }*/
            }

            _dbAccessor.AddBlock(block);
        }

        public int GetBalance()
        {
            var sum = 0;
            foreach (var unspentTransactionOutput in _myUnspentTransactionOutputs)
            {
                var transactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(unspentTransactionOutput.Item1));

                var transaction = _dbAccessor.GetTransactionByHash(transactionHash);
                sum += transaction.Outputs[unspentTransactionOutput.Item2].Value;
            }

            return sum;
        }

        public void RefreshMyUnspentTransactionOutputsFromBlockChain()
        {
            _myUnspentTransactionOutputs = GetMyUnspentTransactionOutputs();
        }

        public void RefreshAllUnspentTransactionOutputsFromBlockChain()
        {
            _allUnspentTransactionOutputs = GetAllUnspentTransactionOutputs();
        }

        public List<Block> GetBlockChain()
        {
            return _dbAccessor.GetAllBlocks();
        }

        private void CreateBlock()
        {
            var transactionPool = _dbAccessor.GetTransactionPool();

            var lastBlock = _dbAccessor.GetLastBlock();

            var newBlock = new Block
            {
                PreviousBlockHeaderHash = lastBlock.GetHeaderHash(),
                TransactionsHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transactionPool)),
                ProofOfWorkCounter = 0,
                Transactions = transactionPool.ToArray(),
            };

            // POW
            var isPowCalculated = CalculatePow(newBlock);
            if (!isPowCalculated)
            {
                return;
            }

            _dbAccessor.AddBlock(newBlock);

            // refresh local cache
            foreach (var trx in transactionPool)
            {
                // remove spent outputs from local cache
                foreach (var input in trx.Inputs)
                {
                    var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
                    if (_myUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                    {
                        _myUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                    }

                    if (_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                    {
                        _allUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                    }
                }
            }

            _dbAccessor.ClearTransactionPool();

            // TODO send block to all nodes
            var blockJson = JsonConvert.SerializeObject(newBlock);
            var blockBytes = Encoding.UTF8.GetBytes(blockJson);
            _host.sendBlock(blockBytes);
        }

        private bool CalculatePow(Block block)
        {
            var pow = new ProofOfWork(SHA256.Create(), PowDifficulty, block.GetHeaderHash());
            var isPowSolutionFound = pow.FindSolution(_cancellationToken);
            if (!isPowSolutionFound)
            {
                return false;
            }

            block.ProofOfWorkCounter = BitConverter.ToInt32(pow.Solution);

            return true;
        }

        private bool IsBlockValid(Block block)
        {
            // check hash prev
            var blck = _dbAccessor.GetBlockByHash(block.PreviousBlockHeaderHash);
            if (blck == null)
            {
                return false;
            }

            // POW
            var pow = new ProofOfWork(SHA256.Create(), PowDifficulty, block.GetHeaderHash());
            var isPowSolutionValid = pow.VerifySolution(BitConverter.GetBytes(block.ProofOfWorkCounter));

            if (!isPowSolutionValid)
            {
                return false;
            }

            // check transactions
            foreach (var transaction in block.Transactions)
            {
                var transactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));
                var trn = _dbAccessor.GetTransactionByHash(transactionHash);

                // Validate when current node doesn't have this transaction already
                if (trn != null)
                {
                    continue;
                }

                if (!IsTransactionValid(transaction))
                {
                    return false;
                }

                // TODO refresh local transaction pool hash 
                // remove spent outputs from local hash
                foreach (var input in transaction.Inputs)
                {
                    var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
                    if (_myUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                    {
                        _myUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                    }

                    if (_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                    {
                        _allUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                    }
                }

                // add new unspent outputs to local hash
                for (var i = 0; i < transaction.Outputs.Length; i++)
                {
                    var output = transaction.Outputs[i];
                    if (output.ScriptPublicKey == _currentNodeKeys)
                    {
                        _myUnspentTransactionOutputs.Add((transaction, i));
                        _allUnspentTransactionOutputs.Add((transaction, i));

                        continue;
                    }

                    _allUnspentTransactionOutputs.Add((transaction, i));
                }
            }

            return true;
        }

        private bool IsTransactionValid(Transaction transaction)
        {
            foreach (var inputToValidate in transaction.Inputs)
            {
                // check for existence
                var trn = _dbAccessor.GetTransactionByHash(inputToValidate.PreviousTransactionHash);
                if (trn == null)
                {
                    return false;
                }

                TransactionOutput output;
                try
                {
                    output = trn.Outputs[inputToValidate.PreviousTransactionOutputIndex];
                }
                catch
                {
                    return false;
                }

                // check for double spending
                if (IsInputDoubleSpent(inputToValidate))
                {
                    return false;
                }

                // signature check and key equality
                var parseArray = inputToValidate.ScriptSignature.Split(" ");
                var (signature, key) = Tuple.Create(parseArray[0], parseArray[1]);

                if (output.ScriptPublicKey != key)
                {
                    return false;
                }

                if (!IsSignatureValid(signature, key, transaction))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSignatureValid(string signature, string key, Transaction transaction)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(key);

            return rsa.VerifyHash(
                Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction)),
                CryptoConfig.MapNameToOID("SHA256"),
                Convert.FromBase64String(signature));
        }

        private bool IsInputDoubleSpent(TransactionInput input)
        {
            var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
            if (!_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
            {
                return true;
            }

            return false;
        }

        private List<(Transaction, int)> GetMyUnspentTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var myTransactionOutputs = GetAllMyTransactionOutputs();
            foreach (var myTransactionOutput in myTransactionOutputs)
            {
                if (IsTransactionOutputUnspent(myTransactionOutput.Item1, myTransactionOutput.Item2))
                {
                    result.Add(myTransactionOutput);
                }
            }

            return result;
        }

        private List<(Transaction, int)> GetAllUnspentTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var allTransactionOutputs = GetAllTransactionOutputs();
            foreach (var transactionOutput in allTransactionOutputs)
            {
                if (IsTransactionOutputUnspent(transactionOutput.Item1, transactionOutput.Item2))
                {
                    result.Add(transactionOutput);
                }
            }

            return result;
        }

        private List<(Transaction, int)> GetAllMyTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var blocks = _dbAccessor.GetAllBlocks();
            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    for (var i = 0; i < transaction.Outputs.Length; i++)
                    {
                        var output = transaction.Outputs[i];
                        if (output.ScriptPublicKey == _currentNodeKeys)
                        {
                            result.Add((transaction, i));
                        }
                    }
                }
            }

            return result;
        }

        private List<(Transaction, int)> GetAllTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var blocks = _dbAccessor.GetAllBlocks();
            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    for (var i = 0; i < transaction.Outputs.Length; i++)
                    {
                        result.Add((transaction, i));
                    }
                }
            }

            return result;
        }

        private bool IsTransactionOutputUnspent(Transaction transaction, int outputIndex)
        {
            var transactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));

            var blocks = _dbAccessor.GetAllBlocks();
            foreach (var block in blocks)
            {
                foreach (var trx in block.Transactions)
                {
                    foreach (var input in transaction.Inputs)
                    {
                        if (input.PreviousTransactionHash == transactionHash &&
                            input.PreviousTransactionOutputIndex == outputIndex)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private (Transaction, int) FindTransactionToSpend(int amount)
        {
            Transaction transactionToSpend = null;
            int transactionToSpendOutputIndex = 0;

            foreach (var unSpentTransactionOutput in _myUnspentTransactionOutputs)
            {
                bool isBlockFound = false;

                for (var i = 0; i < unSpentTransactionOutput.Item1.Outputs.Length; i++)
                {
                    var output = unSpentTransactionOutput.Item1.Outputs[i];
                    if (output.ScriptPublicKey == _currentNodeKeys &&
                            output.Value >= amount)
                    {
                        transactionToSpend = unSpentTransactionOutput.Item1;
                        transactionToSpendOutputIndex = i;

                        isBlockFound = true;
                        break;
                    }
                }

                if (isBlockFound)
                {
                    break;
                }
            }

            return (transactionToSpend, transactionToSpendOutputIndex);
        }

        public void getDataByHeader(MessageHeader header, byte[] data)
        {
            switch (header)
            {
                case MessageHeader.Transaction:
                    {
                        var receivedJson = Encoding.UTF8.GetString(data);
                        var receivedTransaction = JsonConvert.DeserializeObject<Transaction>(receivedJson);
                        Trace.WriteLine($"Transaction received:\n{receivedTransaction}");

                        if (!IsTransactionValid(receivedTransaction))
                        {
                            Trace.WriteLine("Transaction not valid");
                            break;
                        }

                        AddTransactionToPool(receivedTransaction);
                        Trace.WriteLine("Transaction added to pool");

                        break;
                    }
                case MessageHeader.Block:
                    {
                        var receivedJson = Encoding.UTF8.GetString(data);
                        var receivedBlock = JsonConvert.DeserializeObject<Block>(receivedJson);
                        Trace.WriteLine($"Block received:\n{receivedBlock}");

                        if (!IsBlockValid(receivedBlock))
                        {
                            Trace.WriteLine("Block not valid");
                            break;
                        }

                        var existedBlock = _dbAccessor.GetBlockByHash(receivedBlock.GetHeaderHash());
                        if (existedBlock != null)
                        {
                            break;
                        }

                        var transactionPool = _dbAccessor.GetTransactionPool();
                        foreach(var transaction in receivedBlock.Transactions)
                        {
                            if (transactionPool.Contains(transaction))
                            {
                                _cancellationTokenSource.Cancel();
                                break;
                            }
                        }

                        AddBlock(receivedBlock);
                        Trace.WriteLine("Block added to blockchain");

                        break;
                    }
                case MessageHeader.Unknown:
                    {
                        // TODO: 
                        // Unknow header!
                        Trace.WriteLine("UNKNOWN:");
                        Trace.WriteLine(Encoding.UTF8.GetString(data));
                        break;
                    }
            }
        }
    }
}

