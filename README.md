```
docker run --name bitcoind -d --volume /Users/[USER_NAME]/bitcoin_data:/root/.bitcoin -p 127.0.0.1:443:18443 farukter/bitcoind:regtest
docker exec -it bitcoind bash

ADD=$(bitcoin-cli getnewaddress)
bitcoin-cli generatetoaddress 101 $ADD
bitcoin-cli getwalletinfo
bitcoin-cli listunspent

// Run the unit test, note the txId

bitcoin-cli getrawtransaction [TXID]
// Check inputs and outputs

bitcoin-cli generate 1
// Note blockhash = [BLOCKHASH]

bitcoin-cli getblock [BLOCKHASH]
// Note tx hashes => [TXHASH]

bitcoin-cli getrawtransaction [TXHASH] 1
```
