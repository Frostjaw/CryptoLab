namespace CryptoLab.Core.Models
{
    using System;
    using System.Text;

    [Serializable]
    public class TransactionOutput
    {
        /// <summary>
        /// Объём переводимых средств
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Условия траты монет (чаще всего открытый ключ получателя или хэш от него) - адрес
        /// </summary>
        public string ScriptPublicKey { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder()
                .AppendLine($"Value: {Value}")
                .AppendLine($"ScriptPublicKey: {ScriptPublicKey}");

            return sb.ToString();
        }
    }
}
