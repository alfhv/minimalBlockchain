using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using minimalBlockchain.Console.Models;
using Newtonsoft.Json;

namespace minimalBlockchain.Console
{
    public class Transaction
    {
        public string sender { get; set; }
        public string recipient { get; set; }
        public double amount { get; set; }
    }

    public class Block
    {
        public int index { get; set; }
        public double timestamp { get; set; }
        public List<Transaction> transactions { get; set; }
        public int proof { get; set; }
        public string proof_hash { get; set; }
        public string previous_hash { get; set; }
    }

    public class Blockchain
    {
        private static Blockchain _Instance;
        public static Blockchain Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new Blockchain();

                return _Instance;
            }
        }


        public List<Block> Chain;
        private List<Transaction> _CurrentTransactions;
        private HashSet<string> Nodes;

        private Blockchain()
        {
            Nodes = new HashSet<string>();
            Chain = new List<Block>();
            _CurrentTransactions = new List<Transaction>();
            NewBlock(100, "1");
        }

        private void NewBlock(int proof, string previosHash = null, string proof_hash = null)
        {
            var block = new Block
            {
                index = Chain.Count + 1,
                timestamp = DateTime.Now.Ticks,
                transactions = _CurrentTransactions,
                proof = proof,
                proof_hash = proof_hash,
                previous_hash = previosHash ?? Hash(LastBlock)
            };

            Chain.Add(block);

            _CurrentTransactions = new List<Transaction>(); // Reset the current list of transactions
        }

        public int NewTransaction(string sender, string recipient, double amount)
        {
            _CurrentTransactions.Add(new Transaction
            {
                sender = sender,
                recipient = recipient,
                amount = amount
            });

            return LastBlock.index + 1;
        }

        public Block LastBlock
        {
            get
            {
                return Chain.Last();
            }
        }

        class powResult
        {
            public int proof { get; set; }
            public string hash { get; set; }
        }

        private powResult ProofOfWork()
        {
            var proof = 0;
            var guess_hash = ValidProof(LastBlock.proof, proof, LastBlock.previous_hash);
            while (guess_hash == null)
            {
                proof++;
                guess_hash = ValidProof(LastBlock.proof, proof, LastBlock.previous_hash);
            }

            return new powResult { proof = proof, hash = guess_hash };
        }

        public void Mine()
        {
            var pow = ProofOfWork();

            NewTransaction(
                sender: "0",
                recipient: Program.NodeIdentifier,
                amount: 1);

            NewBlock(pow.proof, Hash(LastBlock), pow.hash);
        }

        public int RegisterNode(string url)
        {
            Nodes.Add(url);

            return Nodes.Count;
        }

        private bool ValidChain(List<Block> chain)
        {
            //chain.OrderBy(b => b.Index)
            var last_block = chain[0];
            var current_index = 1;
            while (current_index < chain.Count)
            {
                var block = chain[current_index];
                var previous_block_hash = Hash(last_block); // recalculate hash
                if (block.previous_hash != previous_block_hash)
                {
                    // invalid previous block hash
                    return false;
                }

                // validate PoW
                if (ValidProof(last_block.proof, block.proof, previous_block_hash) == null)
                {
                    return false;
                }

                last_block = block;
                current_index++;
            }

            return true;
        }

        public bool Consensus()
        {
            foreach (var node in Nodes)
            {
                var client = new HttpClient();
                var response = client.GetAsync($"{node}/chain/chain");
                response.Wait();

                ChainResponse chain = null;
                if (response.Result.IsSuccessStatusCode)
                {
                    chain = response.Result.Content.ReadAsAsync<ChainResponse>().Result;
                }

                if (chain == null)
                    return false; // error reading remote chain

                if (chain.Length > LastBlock.index && ValidChain(chain.Chain))
                {
                    // replace our chain
                    Chain = chain.Chain;
                    return true;
                }
            }

            return false;
        }

        private string ValidProof(int lastProof, int proof, string previousHash)
        {
            var guess_hash = sha256_hash($"{lastProof}{proof}{previousHash}");

            return guess_hash.StartsWith("0000") ? guess_hash : null;
        }

        private string Hash(Block block)
        {
            var t_concat = block.transactions.OrderBy(t => new { t.sender, t.recipient, t.amount })
                                          .Select(t => $"{t.sender}{t.recipient}{t.amount}").ToList();
            var t_str = string.Join("", t_concat);

            var b_t = block.timestamp.ToString("R").Replace(".", "").Replace(",", "");

            var to_hash = $"{block.index}{block.previous_hash}{block.proof}{block.proof_hash}{b_t}{t_str}";

            //var block_str = JsonConvert.SerializeObject(block);
            return sha256_hash(to_hash);
        }

        public static string sha256_hash(string proof)
        {
            var sb = new StringBuilder();

            using (var hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(proof));

                foreach (Byte b in result)
                    sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
