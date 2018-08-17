using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using minimalBlockchain.Console.Models;

namespace minimalBlockchain.Console.Controllers
{
    public class ChainController : ApiController
    {
        // GET: /chain
        [HttpGet]
        public ChainResponse Chain()
        {
            return new ChainResponse
            {
                Chain = Blockchain.Instance.Chain,
                Length = Blockchain.Instance.LastBlock.index
            };
        }

        [HttpGet]
        public MineResponse Mine()
        {
            Blockchain.Instance.Mine();

            return new MineResponse
            {
                Message = "New Block Forged",
                MinedBlock = Blockchain.Instance.LastBlock
            };
        }
    }

    public class TransactionsController : ApiController
    {
        // POST: /transactions/new
        [HttpPost]
        public string New(string sender, string recipient, double amount)
        {
            var index = Blockchain.Instance.NewTransaction(sender, recipient, amount);
            return string.Format($"Transaction will be added to Block {index}");
        }
    }
}
