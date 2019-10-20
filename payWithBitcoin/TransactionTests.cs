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
        // RPCClient client;
        // Network network;

        Task<decimal> GetBtcOf1Tl()
        {
            return Task.FromResult<decimal>(0.00001m);
        }

        Dictionary<string, int> GetSalaryList()
        {
           return new Dictionary<string, int>(){
                { "2N2LGTF2XWA5png8mT2fbHRzY9P4XDLWvGc", 2500},
                { "2N94h5bWDAFNsd7SB1f3cQ6RUMmoMaA44tQ", 4500},
                { "2MsjZP4DJmZeV1fRpAw8NcJWiibYopWL7fg", 2250},
                { "2N1cHqXYLas7bnLoCtMppD1yKtrXjRSmB5M", 10000},
                { "2NA9di1yBdHiwgJJVB27nPxpAc8YDpmdSS1", 12500}
            };
        }

        // Dictionary<string, int> salaries;
        // decimal btcPerTl;
        // Money totalPaymentAmountBtc;
        
        [SetUp]
        public async Task Setup() 
        {
           
        }

        [Test]
        public async Task ManuelSteps()
        {
            var network = Network.RegTest;
            var client = new RPCClient(
                credentials: new NetworkCredential(
                    userName: "myuser", 
                    password: "SomeDecentp4ssw0rd"), 
                address: new Uri("http://127.0.0.1:443"),
                network: network);

            Dictionary<string, int> salaries =  GetSalaryList();
            decimal btcPerTl = 0.00001m;

            var totalPayment = salaries.Sum((pair) => pair.Value);
            Money totalPaymentAmountBtc = new Money(totalPayment * btcPerTl, MoneyUnit.BTC);

            var minerFee = new Money(0.000003m, MoneyUnit.BTC);
            totalPaymentAmountBtc += minerFee;

            // Find unspent list for inputs
            var inputList = new List<UnspentCoin>();
            var unspentResponse = await client.ListUnspentAsync();
            var unspentList = unspentResponse.OrderByDescending( x => x.Amount).ToList();
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
            var changeAmount = inputBtcSum - totalPaymentAmountBtc;

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
            var network = Network.RegTest;
            var client = new RPCClient(
                credentials: new NetworkCredential(
                    userName: "myuser", 
                    password: "SomeDecentp4ssw0rd"), 
                address: new Uri("http://127.0.0.1:443"),
                network: network);

            Dictionary<string, int> salaries =  GetSalaryList();
            decimal btcPerTl = 0.00001m;


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

        [Test]
        public async Task TxSelection()
        {
            var network = Network.RegTest;
            var client = new RPCClient(
                credentials: new NetworkCredential(
                    userName: "myuser", 
                    password: "SomeDecentp4ssw0rd"), 
                address: new Uri("http://127.0.0.1:443"),
                network: network);

            Dictionary<string, int> salaries =  GetSalaryList();
            decimal btcPerTl = 0.00001m;

            // Create a transaction
            var transaction = Transaction.Create(network);
            transaction.Inputs.Add(new OutPoint(){
                Hash = new uint256("52560426854bb267b96f6f2fc425b88d86a5c49058e230abb5400b14a9433782"),
                N = 3
            });

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