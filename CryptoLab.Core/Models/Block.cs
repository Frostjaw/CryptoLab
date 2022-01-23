namespace CryptoLab.Core.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;

    [Serializable]
    public class Block : IEquatable<Block>
    {
        /// <summary>
        /// Hash заголовка предыдущего блока
        /// </summary>
        public byte[] PreviousBlockHeaderHash { get; set; }

        /// <summary>
        /// Hash всех транзакций в блоке
        /// </summary>
        public byte[] TransactionsHash { get; set; }

        /// <summary>
        /// Решение proof of work
        /// </summary>
        public int ProofOfWorkCounter { get; set; }

        /// <summary>
        /// Транзакции
        /// </summary>
        [field: NonSerialized]
        public Transaction[] Transactions { get; set; }

        public byte[] GetHeaderHash()
        {
            var hash = Utils.ComputeSha256Hash(PreviousBlockHeaderHash.Concat(TransactionsHash).ToArray());

            return hash;
        }

        public bool Equals([AllowNull] Block block)
        {
            if (block is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, block))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != block.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            var thisHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(this));
            var comparableHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(block));

            return thisHash.SequenceEqual(comparableHash);
        }

        public override string ToString()
        {
            var sb = new StringBuilder()
                .AppendLine($"PreviousBlockHeaderHash: {PreviousBlockHeaderHash}")
                .AppendLine($"TransactionsHash: {TransactionsHash}")
                .AppendLine($"ProofOfWorkCounter: {ProofOfWorkCounter}");

            foreach (var transaction in Transactions)
            {
                sb.AppendLine("Transactions:");
                sb.Append(transaction.ToString());
            }

            return sb.ToString();
        }
    }
}
