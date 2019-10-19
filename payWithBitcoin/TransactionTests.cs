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

        Task<decimal> GetBtcOf1Tl()
        {
            return Task.FromResult<decimal>(0.0001m);
        }

        Task<Dictionary<string, int>> GetSalaryList()
        {
            var salaries = new Dictionary<string, int>(){
                { "2N2LGTF2XWA5png8mT2fbHRzY9P4XDLWvGc", 2500},
                { "2N94h5bWDAFNsd7SB1f3cQ6RUMmoMaA44tQ", 4500},
                { "2MsjZP4DJmZeV1fRpAw8NcJWiibYopWL7fg", 2250},
                { "2N1cHqXYLas7bnLoCtMppD1yKtrXjRSmB5M", 10000},
                { "2NA9di1yBdHiwgJJVB27nPxpAc8YDpmdSS1", 12500}
            };
            return Task.FromResult(salaries);
        }

        Dictionary<string, int> salaries;
        decimal btcPerTl;
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

            salaries = await GetSalaryList();
            btcPerTl = await GetBtcOf1Tl();

            var totalPayment = salaries.Sum((pair) => pair.Value);
            totalPaymentAmountBtc = new Money(totalPayment * btcPerTl, MoneyUnit.BTC);
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
            
            // Create a transaction and add selected inputs 
            var transaction = Transaction.Create(network);
            foreach (var input in inputList)
            {
                transaction.Inputs.Add(input.OutPoint);
            }
            
            // Add outputs
            foreach (var salary in salaries)
            {
                transaction.Outputs.Add(new TxOut()
                {
                    Value = new Money(salary.Value * btcPerTl, MoneyUnit.BTC),
                    ScriptPubKey =  BitcoinAddress.Create(salary.Key, network).ScriptPubKey
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

        [Test]
        public async Task AutoFund()
        {
            // Create a transaction
            var transaction = Transaction.Create(network);
            
            // Add outputs
            foreach (var salary in salaries)
            {
                transaction.Outputs.Add(new TxOut()
                {
                    Value = new Money(salary.Value * btcPerTl, MoneyUnit.BTC),
                    ScriptPubKey =  BitcoinAddress.Create(salary.Key, network).ScriptPubKey
                });
            }

            // Func the transaction 
            var fundTxResponse = await client.FundRawTransactionAsync(transaction);

            // Sign & send the transaction
            var signedTxResponse = await client.SignRawTransactionWithWalletAsync(new SignRawTransactionRequest() { 
                Transaction = fundTxResponse.Transaction,
            });
            var txHash = await client.SendRawTransactionAsync(signedTxResponse.SignedTransaction);
        }
    }
}