using System;
using System.Text;
using NBitcoin;
using QBitNinja.Client;

namespace bitcoinClient.Console
{
    class Program
    {
        static string TestNet_privateKey = "cTBsZLTY87BjTwhzdqRDPnXkfvWFH7pkYB5zKsDQsGL7TT5qef9Y";
        static string TestNet_bitcoinAddress = "n4aL4nx7SjzdvNeqf8MhXFJkVLgo85LxNc";
        static string TestNet_TxId = "a52ec45756f86594bf0231ae893d6ab77c68cc110ac766f30c369190f5acd470";

        static string Testnet_receiverAddress = "mvjXv5QrHR769RcvwaTzPkE4dqLNPgtctn";
        static string Testnet_receiverPrivateKey = "cSvDZdxgir4mZEnsXR4VsNcQ29VePpXab7VSWWkYYhqKFYaapVP7";

        static void Main(string[] args)
        {
            /*
            var privateKey = new Key();
            var testnetPrivateKey = privateKey.GetBitcoinSecret(Network.TestNet);
            System.Console.WriteLine(testnetPrivateKey);

            var testnet_receiver_address = testnetPrivateKey.GetAddress();
              */

            Main_CreateTransaction();

            System.Console.WriteLine("Hello World!");
        }

        static void Main_CreateTransaction()
        {
            var bitcoinPrivateKey = new BitcoinSecret(TestNet_privateKey);
            var network = bitcoinPrivateKey.Network;
            var address = bitcoinPrivateKey.GetAddress();

            System.Console.WriteLine("bitcoinPrivateKey: " + bitcoinPrivateKey);
            System.Console.WriteLine("address calculated: " + address);
            System.Console.WriteLine("address expected  : " + TestNet_bitcoinAddress);

            TestNet_TxId = "08e88a752f165be9ee31155b79038052d2d5710f561bbb2a8c082bbab92cbf0d";// "dba5eaed767c96365ace964ded6d3c1fa50a7c545262c95edc20005a5052e350";
            var client = new QBitNinjaClient(network);

            var transactionId = uint256.Parse(TestNet_TxId);
            var transactionResponse = client.GetTransaction(transactionId).Result;

            if (transactionResponse.Block == null || transactionResponse.Block == null || transactionResponse.Block.Confirmations == 0)
            {
                System.Console.WriteLine("Transaction not Confirmed yet");
            }

            System.Console.WriteLine("TransactionId: " + transactionResponse.TransactionId);
            System.Console.WriteLine("Confirmations: " + transactionResponse.Block.Confirmations);

            var receivedCoins = transactionResponse.ReceivedCoins;

            ICoin coinToSpend = null;

            foreach (var coin in receivedCoins)
            {
                System.Console.Write("ReceivedCoins: " + coin.Amount);
                if (coin.TxOut.ScriptPubKey == bitcoinPrivateKey.ScriptPubKey)
                {
                    coinToSpend = coin;
                    System.Console.WriteLine("...belong to us.");
                }
                else System.Console.WriteLine("...NOT ours.");

                if ((Money)coin.Amount == Money.Zero)
                {
                    System.Console.WriteLine($"found a custom msg:{Encoding.UTF8.GetString(coin.GetScriptCode().ToBytes())}");
                }
            }

            var transaction = Transaction.Create(bitcoinPrivateKey.Network);
            transaction.AddInput(new TxIn()
            {
                PrevOut = coinToSpend.Outpoint
            });           
            
            var receiverAddress = BitcoinAddress.Create("mzp4No5cmCXjZUpf112B1XWsvWBfws5bbB", Network.TestNet); // receiver address
            var moneyToReceiver = new Money(0.01m, MoneyUnit.BTC);
            var minerFee = new Money(0.0001m, MoneyUnit.BTC);

            var txInAmount = (Money)coinToSpend.Amount;
            Money changeBackAmount = txInAmount - moneyToReceiver - minerFee;

            TxOut receiverTxOut = new TxOut()
            {
                Value = moneyToReceiver,
                ScriptPubKey = receiverAddress.ScriptPubKey
            };

            TxOut changeBackTxOut = new TxOut()
            {
                Value = changeBackAmount,
                ScriptPubKey = bitcoinPrivateKey.ScriptPubKey
            };

            transaction.Outputs.Add(receiverTxOut);
            transaction.Outputs.Add(changeBackTxOut);

            var message = "ahv testing BC!";
            var bytes = Encoding.UTF8.GetBytes(message);
            transaction.Outputs.Add(new TxOut()
            {
                Value = Money.Zero,
                ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(bytes)
            });

            transaction.Inputs[0].ScriptSig = bitcoinPrivateKey.ScriptPubKey;

            transaction.Sign(secret: bitcoinPrivateKey, assumeP2SH: false);

            var broadcastResponse = client.Broadcast(transaction).Result;

            if (!broadcastResponse.Success || broadcastResponse.Error != null)
            {
                System.Console.Error.WriteLine("ErrorCode: " + broadcastResponse.Error.ErrorCode);
                System.Console.Error.WriteLine("Error message: " + broadcastResponse.Error.Reason);
            }
            else
            {
                System.Console.WriteLine("Success! You can check out the hash of the transaciton in any block explorer:");
                System.Console.WriteLine("hash: " + transaction.GetHash());
            }
        }
    }
}
