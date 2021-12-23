namespace CryptoLab.Core.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    [Serializable]
    public class Transaction : IEquatable<Transaction>
    {
        /// <summary>
        /// Входы
        /// </summary>
        public TransactionInput[] Inputs { get; set; }

        /// <summary>
        /// Выходы
        /// </summary>
        public TransactionOutput[] Outputs { get; set; }

        public bool Equals([AllowNull] Transaction transaction)
        {
            if (transaction is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, transaction))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != transaction.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            var thisHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(this));
            var comparableHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));

            return thisHash.SequenceEqual(comparableHash);
        }
    }
}
