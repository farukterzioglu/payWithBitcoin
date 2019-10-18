```
docker run --name bitcoind -d --volume /Users/[USER_NAME]/bitcoin_data:/root/.bitcoin -p 127.0.0.1:443:18443 farukter/bitcoind:regtest
docker exec -it bitcoind bash

ADD=$(bitcoin-cli getnewaddress)
bitcoin-cli generatetoaddress 101 $ADD
bitcoin-cli getwalletinfo
bitcoin-cli listunspent
```
