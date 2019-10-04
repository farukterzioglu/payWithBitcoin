using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;

namespace payWithBitcoin
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var unspentList = await GetUnspentList();
            var salary = await GetSalary();
            var satoshis = await GetSatoshisOfTl(salary);
            var addressList = GetAddresses();

            var totalPaymentAmount = addressList.Count * satoshis;
            
            var inputList = new List<UnspentCoin>();
            unspentList = unspentList.OrderByDescending( x => x.Amount.Satoshi).ToList();
            long inputSum = 0;
            unspentList.TakeWhile( x => {
                var temp = inputSum; 
                inputSum += x.Amount.Satoshi; 
                inputList.Add(x);
                return temp < totalPaymentAmount;
            });

            CreateTx(inputList, addressList, satoshis );
        }

        static void CreateTx(List<UnspentCoin> inputList, List<string> addressList, int salarySatoshi)
        {
            
        }

        static async Task<List<UnspentCoin>> GetUnspentList()
        {
            Network network = Network.RegTest;

            RPCClient client = new RPCClient(
                credentials: new NetworkCredential(userName: "myuser", password: "SomeDecentp4ssw0rd"), 
                address: new Uri("http://localhost:18443"),
                network: network);

            
            var unspentList = await client.ListUnspentAsync();
            return unspentList.ToList();
        }

        static Task<int> GetSatoshisOfTl(int salary)
        {
            return Task.FromResult<int>(10 * salary);
        }

        static Task<int> GetSalary()
        {
            return Task.FromResult(1);
        }

        static List<string> GetAddresses()
        {
            return new List<string>(){
                "2N2LGTF2XWA5png8mT2fbHRzY9P4XDLWvGc",
                "2N94h5bWDAFNsd7SB1f3cQ6RUMmoMaA44tQ",
                "2MsjZP4DJmZeV1fRpAw8NcJWiibYopWL7fg",
                "2N1cHqXYLas7bnLoCtMppD1yKtrXjRSmB5M",
                "2NA9di1yBdHiwgJJVB27nPxpAc8YDpmdSS1"    
            };
        }
    }
}
