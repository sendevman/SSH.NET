﻿using System;

namespace Renci.SshNet.Security.Cryptography.Ciphers
{
    /// <summary>
    /// Implements ARCH4 cipher algorithm
    /// </summary>
    public sealed class Arc4Cipher : StreamCipher
    {
        private static readonly int STATE_LENGTH = 256;

        /// <summary>
        ///  Holds the state of the RC4 engine
        /// </summary>
        private byte[] _engineState;

        private int _x;

        private int _y;

        /// <summary>
        /// Gets the minimum data size.
        /// </summary>
        /// <value>
        /// The minimum data size.
        /// </value>
        public override byte MinimumSize
        {
            get { return 0; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Arc4Cipher" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="dischargeFirstBytes">if set to <see langword="true"/> will disharged first 1536 bytes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null"/>.</exception>
        public Arc4Cipher(byte[] key, bool dischargeFirstBytes)
            : base(key)
        {
            SetKey(key);

            // The first 1536 bytes of keystream generated by the cipher MUST be discarded, and the first byte of the
            // first encrypted packet MUST be encrypted using the 1537th byte of keystream.
            if (dischargeFirstBytes)
            {
                _ = Encrypt(new byte[1536]);
            }
        }

        /// <summary>
        /// Encrypts the specified region of the input byte array and copies the encrypted data to the specified region of the output byte array.
        /// </summary>
        /// <param name="inputBuffer">The input data to encrypt.</param>
        /// <param name="inputOffset">The offset into the input byte array from which to begin using data.</param>
        /// <param name="inputCount">The number of bytes in the input byte array to use as data.</param>
        /// <param name="outputBuffer">The output to which to write encrypted data.</param>
        /// <param name="outputOffset">The offset into the output byte array from which to begin writing data.</param>
        /// <returns>
        /// The number of bytes encrypted.
        /// </returns>
        public override int EncryptBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return ProcessBytes(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        /// <summary>
        /// Decrypts the specified region of the input byte array and copies the decrypted data to the specified region of the output byte array.
        /// </summary>
        /// <param name="inputBuffer">The input data to decrypt.</param>
        /// <param name="inputOffset">The offset into the input byte array from which to begin using data.</param>
        /// <param name="inputCount">The number of bytes in the input byte array to use as data.</param>
        /// <param name="outputBuffer">The output to which to write decrypted data.</param>
        /// <param name="outputOffset">The offset into the output byte array from which to begin writing data.</param>
        /// <returns>
        /// The number of bytes decrypted.
        /// </returns>
        public override int DecryptBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return ProcessBytes(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        /// <summary>
        /// Encrypts the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="offset">The zero-based offset in <paramref name="input"/> at which to begin encrypting.</param>
        /// <param name="length">The number of bytes to encrypt from <paramref name="input"/>.</param>
        /// <returns>
        /// Encrypted data.
        /// </returns>
        public override byte[] Encrypt(byte[] input, int offset, int length)
        {
            var output = new byte[length];
            _ = ProcessBytes(input, offset, length, output, 0);
            return output;
        }

        /// <summary>
        /// Decrypts the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        /// The decrypted data.
        /// </returns>
        public override byte[] Decrypt(byte[] input)
        {
            return Decrypt(input, 0, input.Length);
        }

        /// <summary>
        /// Decrypts the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="offset">The zero-based offset in <paramref name="input"/> at which to begin decrypting.</param>
        /// <param name="length">The number of bytes to decrypt from <paramref name="input"/>.</param>
        /// <returns>
        /// The decrypted data.
        /// </returns>
        public override byte[] Decrypt(byte[] input, int offset, int length)
        {
            var output = new byte[length];
            _ = ProcessBytes(input, offset, length, output, 0);
            return output;
        }

        private int ProcessBytes(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if ((inputOffset + inputCount) > inputBuffer.Length)
            {
                throw new IndexOutOfRangeException("input buffer too short");
            }

            if ((outputOffset + inputCount) > outputBuffer.Length)
            {
                throw new IndexOutOfRangeException("output buffer too short");
            }

            for (var i = 0; i < inputCount; i++)
            {
                _x = (_x + 1) & 0xff;
                _y = (_engineState[_x] + _y) & 0xff;

                // swap
                var tmp = _engineState[_x];
                _engineState[_x] = _engineState[_y];
                _engineState[_y] = tmp;

                // xor
                outputBuffer[i + outputOffset] = (byte)(inputBuffer[i + inputOffset] ^ _engineState[(_engineState[_x] + _engineState[_y]) & 0xff]);
            }

            return inputCount;
        }

        private void SetKey(byte[] keyBytes)
        {
            _x = 0;
            _y = 0;

            _engineState ??= new byte[STATE_LENGTH];

            // reset the state of the engine
            for (var i = 0; i < STATE_LENGTH; i++)
            {
                _engineState[i] = (byte) i;
            }

            var i1 = 0;
            var i2 = 0;

            for (var i = 0; i < STATE_LENGTH; i++)
            {
                i2 = ((keyBytes[i1] & 0xff) + _engineState[i] + i2) & 0xff;

                // do the byte-swap inline
                var tmp = _engineState[i];
                _engineState[i] = _engineState[i2];
                _engineState[i2] = tmp;
                i1 = (i1 + 1) % keyBytes.Length;
            }
        }
    }
}
