using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using minimalBlockchain.Console.Models;

namespace minimalBlockchain.Console.Controllers
{
    public class NodeController : ApiController
    {
        [HttpPost]
        public RegisterResponse Register(string url)
        {
            var count = Blockchain.Instance.RegisterNode(url);

            return new RegisterResponse
            {
                Message = "Node added succesfully",
                TotalNodes = count
            };
        }

        [HttpGet]
        public ResponseConsensus Consensus()
        {
            var result = Blockchain.Instance.Consensus();

            var response = new ResponseConsensus
            {
                NewChain = new ChainResponse
                {
                    Chain = Blockchain.Instance.Chain,
                    Length = Blockchain.Instance.LastBlock.index
                }
            };

            if (result)
                response.Message = "Our chain was replaced";
            else
                response.Message = "Our chain is authoritative";

            return response;
        }
    }
}
