namespace bitcoinClient.Console
{
  class Program
    {
        static string TestNet_privateKey = "??";
        static string TestNet_bitcoinAddress = "n4aL4nx7SjzdvNeqf8MhXFJkVLgo85LxNc";
        static string TestNet_TxId = "a52ec45756f86594bf0231ae893d6ab77c68cc110ac766f30c369190f5acd470";
        
        static void Main_CreateTransaction(string[] args)
        {
            var bitcoinPrivateKey = new BitcoinSecret(TestNet_privateKey);
            var network = bitcoinPrivateKey.Network;
            var address = bitcoinPrivateKey.GetAddress();

            Console.WriteLine("bitcoinPrivateKey: " + bitcoinPrivateKey); 
            Console.WriteLine("address calculated: " + address);
            Console.WriteLine("address expected  : " + TestNet_bitcoinAddress);

            TestNet_TxId = "781464f497dca89c40ed9c8329c80045a92bc822ef4b81ff30b9bff67a5201d0";
            var client = new QBitNinjaClient(network);

            var transactionId = uint256.Parse(TestNet_TxId);
            var transactionResponse = client.GetTransaction(transactionId).Result;

            if (transactionResponse.Block == null || transactionResponse.Block.Confirmations == 0)
            {
                Console.WriteLine("Transaction not Confirmed yet");
            }

            Console.WriteLine("TransactionId: " + transactionResponse.TransactionId); 
            Console.WriteLine("Confirmations: " + transactionResponse.Block.Confirmations);

            var receivedCoins = transactionResponse.ReceivedCoins;
            OutPoint outPointToSpend = null;

            foreach (var coin in receivedCoins)
            {
                Console.Write("ReceivedCoins: " + coin.Amount);
                if (coin.TxOut.ScriptPubKey == bitcoinPrivateKey.ScriptPubKey)
                {
                    outPointToSpend = coin.Outpoint;
                    Console.WriteLine("...belong to us.");
                } else Console.WriteLine("...NOT ours.");
            }

            var transaction = new Transaction();
            transaction.Inputs.Add(new TxIn()
            {
                PrevOut = outPointToSpend
            });

            var receiverAddress = BitcoinAddress.Create("mzp4No5cmCXjZUpf112B1XWsvWBfws5bbB"); // receiver address
            var moneyToReceiver = new Money(0.5m, MoneyUnit.BTC);
            var minerFee = new Money(0.0001m, MoneyUnit.BTC);

            var txInAmount = (Money)receivedCoins[(int)outPointToSpend.N].Amount;
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
            transaction.Sign(bitcoinPrivateKey, false);

            var broadcastResponse = client.Broadcast(transaction).Result;

            if (!broadcastResponse.Success)
            {
                Console.Error.WriteLine("ErrorCode: " + broadcastResponse.Error.ErrorCode);
                Console.Error.WriteLine("Error message: " + broadcastResponse.Error.Reason);
            }
            else
            {
                Console.WriteLine("Success! You can check out the hash of the transaciton in any block explorer:");
                Console.WriteLine("hash: " + transaction.GetHash());
            }
        }
        
static void Main_ExploreTransaction(string[] args)
        {
            var client = new QBitNinjaClient(Network.TestNet);

            var transactionId = uint256.Parse("94b56c61e3cee09bc8c18fe22dc695e59b80aea5c0fe1a1c31d64cd09dc6af6d");
            var transactionResponse = client.GetTransaction(transactionId).Result;
            var transaction = transactionResponse.Transaction;

            Console.WriteLine(transactionResponse.TransactionId);
            Console.WriteLine(transaction.GetHash());

            var outputs = transaction.Outputs;
            foreach (TxOut output in outputs)
            {
                Money amount = output.Value;

                Console.WriteLine($"amount: {amount.ToDecimal(MoneyUnit.BTC)}");
                var paymentScript = output.ScriptPubKey;
                Console.WriteLine($"ScriptPubKey: {paymentScript}");  // It's the ScriptPubKey
                var address = paymentScript.GetDestinationAddress(Network.TestNet);
                Console.WriteLine($"address: {address}");
                Console.WriteLine();
            }

            var inputs = transaction.Inputs;
            foreach (TxIn input in inputs)
            {
                OutPoint previousOutpoint = input.PrevOut;
                Console.WriteLine($"previousOutpoint.Hash: {previousOutpoint.Hash}"); // hash of prev tx
                Console.WriteLine($"previousOutpoint.N: {previousOutpoint.N}"); // idx of out from prev tx, that has been spent in the current tx
                Console.WriteLine();
            }

        }        
      }
}        
