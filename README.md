A unity plugin that allows you using unipass with webview page.

## Requirements

- UnityHub Verison: 2021.2.11f1
- Vuplex for Android, Vuplex for iOS
- Nethereum v4.8.0

## Installation

## Usage

### interface
```C#
public Wallet walletpublic Wallet wallet;public class WalletConfig
{
    public enum Environment
    {
        testnet, mainnet,
    }

    public enum ChainType
    {
        polygon, eth, bsc, rangers,
    }

    public enum Theme
    {
        dark, light,
    }

    public string nodeRPC = "https://node.wallet.unipass.id/polygon-mumbai";

    public string appIcon = "";

    public ChainType chainType = ChainType.polygon;

    public string domain = "t.wallet.unipass.id";

    public string protocol = "https";

    public Environment env = Environment.testnet;

    public string appName = "";

    public Theme theme = Theme.dark;
}
```

For more information please visit demo script.