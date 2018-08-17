using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace minimalBlockchain.Console.Models
{
    public class BitcoinHeader
    {
        public string Version { get; set; }
        public long Timestamp { get; set; }
        public string TransactionRootHash { get; set; }
        public string PreviousHash { get; set; }
        public long PoWNonce { get; set; } // value to find that satisfy: Hash(Header) < PowTarget
        public ulong PoWTarget { get; set; } // 0 - 2^224
    }

    public class Pow
    {
        public ulong Mine(BitcoinHeader block)
        {
            block.PoWNonce = 0;
            var hash_block = HashNumber(block);
            var target = block.PoWTarget;
            while (hash_block > target)
            {
                block.PoWNonce++;
                hash_block = HashNumber(block);
            }

            return hash_block;
        }

        /// <summary>
        /// </summary>
        /// <param name="block"></param>
        /// <returns>64 char string as byte array = 67 digits number</returns>
        public static ulong HashNumber(BitcoinHeader block)
        {
            var hash = Hash(block);
            return HashNumber(hash);
        }

        public static ulong HashNumber(string exaStr)
        {
            using (var hash = SHA256.Create())
            {
                var str_bytes = Encoding.UTF8.GetBytes(exaStr);
                str_bytes = str_bytes.OrderBy(b => b).ToArray();

                /*
                var tmp = new byte[str_bytes.Length + 1];
                Array.Copy(str_bytes, tmp, str_bytes.Length);

                var bytes = hash.ComputeHash(tmp);
                */

                var bytes = hash.ComputeHash(str_bytes);

                return BitConverter.ToUInt64(bytes, 0);
            }
        }

        public static string Hash(BitcoinHeader block)
        {
            var block_str = JsonConvert.SerializeObject(block);
            return Blockchain.sha256_hash(block_str);
        }
    }
}
