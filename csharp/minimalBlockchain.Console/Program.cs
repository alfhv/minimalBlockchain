using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using minimalBlockchain.Console.App_Start;
using minimalBlockchain.Console.Models;

namespace minimalBlockchain.Console
{
    class Program
    {
        public static string NodeIdentifier = Guid.NewGuid().ToString().Replace("-", string.Empty);

        static void Main(string[] args)
        {

            /*
            var b = new BitcoinHeader
            {
                Version = "0.1.0",
                Timestamp = DateTime.Now.Ticks,
                PreviousHash = "fe6cd7c89aa75c0cec6078fef3730915c228772f18517d2d2cffcbccfcee7f67",
                TransactionRootHash = "0000072884805265936b80446dc25bb4ffdf023aa825f94710284221eb57169c",
                PoWTarget = long.MaxValue / 2,
                PoWNonce = 0,
            };

            var pow = new Pow();

            for (int i = 1; i <= 100; i++)
            {
                var r = pow.Mine(b);
                System.Console.WriteLine($"Nonce: {b.PoWNonce}");
                if (Pow.HashNumber(b) > b.PoWTarget)
                {
                    throw new Exception("Invalid PoW.");
                }
                System.Console.WriteLine("PoW validated.");

                b.PoWTarget = r;
                b.PreviousHash = Pow.Hash(b);

                System.Console.WriteLine("...next iteration...?");
                System.Console.ReadLine();
            }


            var t1 = new Transaction
            {
                sender = "abc",
                recipient = "def",
                amount = 5
            };
            var t2 = new Transaction
            {
                sender = "abc",
                recipient = "ddf",
                amount = 5
            };

            var t_str = $"{t1.sender}{t1.recipient}{t1.amount}";
            var h = sha256_hash(t_str);
            */
            var port = "5001";

            if (args[0] == "-port" || args[0] == "-p")
            {
                port = args[1];
            }

            var url = $"http://localhost:{port}";
            using (WebApp.Start<Startup>(url))
            {
                System.Console.WriteLine("Server started at:" + url);
                System.Console.ReadLine();
            }
        }
    }
}
