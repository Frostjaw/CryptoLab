namespace CryptoLab.Core
{
    using CryptoLab.Core.Models;
    using LiteDB;
    using System.Collections.Generic;
    using System.Linq;

    public class DbAccessor
    {
        private const string DB_NAME = @"Crypto.db";
        private const string BLOCKS_TABLE_NAME = "blocks";
        private const string TRANSACTION_POOL_TABLE_NAME = "transaction_pool";

        public void ClearDb()
        {
            using var db = new LiteDatabase(DB_NAME);

            db.GetCollection<Block>(BLOCKS_TABLE_NAME).DeleteAll();
            db.GetCollection<Transaction>(TRANSACTION_POOL_TABLE_NAME).DeleteAll();
        }

        public void ClearTransactionPool()
        {
            using var db = new LiteDatabase(DB_NAME);
            db.GetCollection<Transaction>(TRANSACTION_POOL_TABLE_NAME).DeleteAll();
        }

        public void AddBlock(Block block)
        {
            using var db = new LiteDatabase(DB_NAME);

            var blocksCollection = db.GetCollection<Block>(BLOCKS_TABLE_NAME);
            blocksCollection.Insert(block);
        }

        public void AddTransactionToPool(Transaction transaction)
        {
            using var db = new LiteDatabase(DB_NAME);

            var transactionPoolCollection = db.GetCollection<Transaction>(TRANSACTION_POOL_TABLE_NAME);
            transactionPoolCollection.Insert(transaction);
        }

        public List<Transaction> GetTransactionPool()
        {
            using var db = new LiteDatabase(DB_NAME);

            var transactionPoolCollection = db.GetCollection<Transaction>(TRANSACTION_POOL_TABLE_NAME);
            var transactions = transactionPoolCollection.FindAll().ToList();

            return transactions;
        }

        public Block GetLastBlock()
        {
            using var db = new LiteDatabase(DB_NAME);

            var blocksCollection = db.GetCollection<Block>(BLOCKS_TABLE_NAME);
            var lastBlock = blocksCollection.FindOne(Query.All(Query.Descending));

            return lastBlock;
        }

        public List<Block> GetAllBlocks()
        {
            using var db = new LiteDatabase(DB_NAME);

            var blocksCollection = db.GetCollection<Block>(BLOCKS_TABLE_NAME);
            var blocks = blocksCollection.FindAll().ToList();

            return blocks;
        }

        public Transaction GetTransactionByHash(byte[] hash)
        {
            using var db = new LiteDatabase(DB_NAME);
            var blocksCollection = db.GetCollection<Block>("blocks");
            var blocks = blocksCollection.FindAll();

            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    var currentTransactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));
                    if (currentTransactionHash.SequenceEqual(hash))
                    {
                        return transaction;
                    }
                }
            }

            var transactionPoolCollection = db.GetCollection<Transaction>(TRANSACTION_POOL_TABLE_NAME);
            var transactions = transactionPoolCollection.FindAll().ToList();
            foreach (var transaction in transactions)
            {
                var currentTransactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));
                if (currentTransactionHash.SequenceEqual(hash))
                {
                    return transaction;
                }
            }

            return null;
        }

        public Block GetBlockByHash(byte[] hash)
        {
            using var db = new LiteDatabase(DB_NAME);
            var blocksCollection = db.GetCollection<Block>("blocks");
            var blocks = blocksCollection.FindAll();

            foreach (var block in blocks)
            {
                var currentBlockHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(block));

                if (currentBlockHash.SequenceEqual(hash))
                {
                    return block;
                }
            }

            return null;
        }
    }
}

