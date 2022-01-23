namespace CryptoLab.Core
{
    using System;
    using System.Security.Cryptography;
    using System.Threading;

    public class ProofOfWork
    {
        public byte[] Challenge
        {
            get { return _challenge; }
        }

        public byte[] Solution
        {
            get { return _solution; }
        }

        public byte Difficulty
        {
            get { return _difficulty; }
        }

        private byte[] _solution;
        private byte[] _challenge;
        private byte _difficulty;

        private HashAlgorithm _hash;

        public ProofOfWork(HashAlgorithm hashAlgorithm, byte difficulty, byte[] challenge = null)
        {
            Initialize(hashAlgorithm, difficulty, challenge);
        }

        private void Initialize(HashAlgorithm hashAlgorithm, byte difficulty, byte[] challenge)
        {
            _hash = hashAlgorithm;
            _difficulty = difficulty;
            _challenge = challenge;
        }

        public bool FindSolution(CancellationToken cancellationToken)
        {
            if (_solution != null)
            {
                return true;
            }

            byte[] hash = null;
            byte[] buffer = new byte[4 + _challenge.Length];

            uint maxCounter = GetMaxCounter(_difficulty);

            Buffer.BlockCopy(_challenge, 0, buffer, 4, _challenge.Length);

            for (uint i = 0; i < maxCounter; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                unsafe
                {
                    fixed (byte* ptr = &buffer[0])
                    {
                        *((uint*)ptr) = i;
                    }
                }

                hash = _hash.ComputeHash(buffer);

                if (CountLeadingZeroBits(hash, _difficulty) >= _difficulty)
                {
                    _solution = new byte[4];
                    Buffer.BlockCopy(buffer, 0, _solution, 0, _solution.Length);

                    return true;
                }
            }

            return false;
        }

        public bool VerifySolution(byte[] solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (solution.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(solution));
            }

            byte[] buffer = new byte[solution.Length + _challenge.Length];

            Buffer.BlockCopy(solution, 0, buffer, 0, solution.Length);
            Buffer.BlockCopy(_challenge, 0, buffer, solution.Length, _challenge.Length);

            byte[] hash = _hash.ComputeHash(buffer);

            return CountLeadingZeroBits(hash, _difficulty) >= _difficulty;
        }

        private static uint GetMaxCounter(int bits)
        {
            return uint.MaxValue;
            //return (uint)Math.Pow(2, bits) * 3;
        }

        private static int CountLeadingZeroBits(byte[] data, int limit)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int zeros = 0;
            byte value = 0;

            for (int i = 0; i < data.Length; i++)
            {
                value = data[i];

                if (value == 0)
                {
                    zeros += 8;
                }
                else
                {
                    int count = 1;

                    if (value >> 4 == 0) { count += 4; value <<= 4; }
                    if (value >> 6 == 0) { count += 2; value <<= 2; }

                    zeros += count - (value >> 7);

                    break;
                }

                if (zeros >= limit)
                {
                    break;
                }
            }

            return zeros;
        }
    }
}
