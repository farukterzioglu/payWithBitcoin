using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using NUnit.Framework;

namespace Tests
{
    public class TransactionTests
    {
        RPCClient client;
        Network network;

        Task<decimal> GetBtcOfTl(int salary)
        {
            return Task.FromResult<decimal>(0.0001m * salary);
        }

        Task<int> GetSalary()
        {
            return Task.FromResult(1);
        }

        List<string> GetAddresses()
        {
            return new List<string>(){
                "2N2LGTF2XWA5png8mT2fbHRzY9P4XDLWvGc",
                "2N94h5bWDAFNsd7SB1f3cQ6RUMmoMaA44tQ",
                "2MsjZP4DJmZeV1fRpAw8NcJWiibYopWL7fg",
                "2N1cHqXYLas7bnLoCtMppD1yKtrXjRSmB5M",
                "2NA9di1yBdHiwgJJVB27nPxpAc8YDpmdSS1"    
            };
        }

        int salary;
        decimal btcSalary;
        List<string> addressList;
        Money totalPaymentAmountBtc;
        
        [SetUp]
        public async Task Setup()
        {
            network = Network.RegTest;
            client = new RPCClient(
                credentials: new NetworkCredential(
                    userName: "myuser", 
                    password: "SomeDecentp4ssw0rd"), 
                address: new Uri("http://127.0.0.1:443"),
                network: network);

            salary = await GetSalary();
            btcSalary = await GetBtcOfTl(salary);
            addressList = GetAddresses();

            totalPaymentAmountBtc = new Money(addressList.Count * btcSalary, MoneyUnit.BTC);
        }

        [Test]
        public async Task ManuelSteps()
        {
            // Find unspent list for inputs
            var inputList = new List<UnspentCoin>();
            var unspentList = (await client.ListUnspentAsync()).OrderByDescending( x => x.Amount).ToList();
            var inputBtcSum = new Money(0, MoneyUnit.BTC);
            foreach (var unspent in unspentList) {
                if( inputBtcSum >= totalPaymentAmountBtc) break;

                inputBtcSum += unspent.Amount;
                inputList.Add(unspent);
            }
            if(inputBtcSum < totalPaymentAmountBtc) {
                throw new Exception("Balance is not enough!");
            }
            
            // Create the transactionand add selected inputs 
            var transaction = Transaction.Create(network);
            foreach (var input in inputList)
            {
                transaction.Inputs.Add(input.OutPoint);
            }
            
            // Add outputs
            foreach (var address in addressList)
            {
                transaction.Outputs.Add(new TxOut()
                {
                    Value = new Money(btcSalary, MoneyUnit.BTC),
                    ScriptPubKey =  BitcoinAddress.Create(address, Network.RegTest).ScriptPubKey
                });
            }

            // Calculate miner fee and change
            var minerFee = new Money(0.00007m, MoneyUnit.BTC);
            var changeAmount = inputBtcSum - totalPaymentAmountBtc - minerFee;

            // Create an output for change
            var changeAddress = await client.GetRawChangeAddressAsync();
            TxOut changeTxOut = new TxOut()
            {
                Value = changeAmount,
                ScriptPubKey = changeAddress.ScriptPubKey
            };
            transaction.Outputs.Add(changeTxOut);

            // Sign & send the transaction
            var signedTxResponse = await client.SignRawTransactionWithWalletAsync(new SignRawTransactionRequest() { 
                Transaction = transaction,
            });
            var txHash = await client.SendRawTransactionAsync(signedTxResponse.SignedTransaction);
        }
    
    
    }
}