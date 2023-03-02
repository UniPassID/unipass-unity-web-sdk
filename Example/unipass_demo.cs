using System.Linq;
using System.Numerics;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Nethereum.Web3;
using UnipassWallet;
using UnityEngine;
using static Nethereum.Util.UnitConversion;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class unipass_demo : MonoBehaviour
{
    public Wallet wallet;

    [SerializeField]
    private TMP_Text usdc_balance;

    [SerializeField]
    private TMP_Text account_text;

    [SerializeField]
    private TMP_Text sig_text;

    [SerializeField]
    private TMP_Text is_valid_sig_text;

    [SerializeField]
    private TMP_Text native_hash;

    [SerializeField]
    private TMP_Text erc20_hash;

    private int chainType = 0;

    public int dropDownValue
    {
        get { return chainType; }
        set
        {
            chainType = value;
            onDropdownChange(value);
        }
    }

    public int dropDownConnectValue
    {
        set
        {
            Connect(value);
        }
    }

    public string message { get; set; }

    // native token inputs
    public string native_to { get; set; }
    public string native_value { get; set; }

    // erc20 token inputs
    public string erc20_to { get; set; }
    public string erc20_value { get; set; }

    private void Awake()
    {
        Debug.Log("unipass_demo awake");
    }

    // connect first 
    public async void Connect(int connectNum)
    {
        Debug.Log("connect clicked");
        WalletConfig.ConnectType connectType;
        if (connectNum == 1)
        {
            connectType = WalletConfig.ConnectType.google;
        }
        else if (connectNum == 2)
        {
            connectType = WalletConfig.ConnectType.email;
        }
        else if (connectNum == 3)
        {
            connectType = WalletConfig.ConnectType.both;
        }
        else return;
        var account = await wallet.Connect(connectType, true);
        Debug.Log(account.address);
        Debug.Log(account.email);
        Debug.Log(account.newborn);
        Debug.Log(account.message);
        Debug.Log(account.signature);
        account_text.text = "address: " + account.address + "email: " + account.email + "newborn: " + account.newborn;
        var config = wallet.GetWalletConfig();
        if (config.chainType == WalletConfig.ChainType.polygon)
        {
            var rpcUrl = "https://node.wallet.unipass.id/polygon-mumbai";
            getUsdcBalance("0x87F0E95E11a49f56b329A1c143Fb22430C07332a", rpcUrl, 6);
        }
        else if (config.chainType == WalletConfig.ChainType.bsc)
        {
            var rpcUrl = "https://node.wallet.unipass.id/bsc-testnet";
            getUsdcBalance("0x64544969ed7EBf5f083679233325356EbE738930", rpcUrl, 18);
        }
        else if (config.chainType == WalletConfig.ChainType.rangers)
        {
            var rpcUrl = "https://node.wallet.unipass.id/rangers-robin";
            getUsdcBalance("0xd6ed1c13914ff1b08737b29de4039f542162cae1", rpcUrl, 6);
        }
    }

    // sign message
    public async void SignMessage()
    {
        if (message == null) return;
        Debug.Log("sign message clicked:");
        Debug.Log(message);
        var sig = await wallet.SignMessage(message);
        Debug.Log(sig);
        sig_text.text = sig;
    }

    public async void VerifyMessage()
    {
        if (message == null) return;
        if (sig_text.text == null || sig_text.text == "") return;

        var isValid = await wallet.isValidSignature(message, sig_text.text);
        Debug.Log("isValid:" + isValid);
        is_valid_sig_text.text = "isValid:" + isValid;
    }

    // send native token
    public async void SendNativeToken()
    {
        if (native_to == null || native_value == null) return;
        Debug.Log("send trnasaction token");
        var tx = new TransactionMessage(from: wallet.getAddress(), to: native_to, value: Web3.Convert.ToWei(BigDecimal.Parse(native_value), 18).ToString(), data: "0x");
        var hash = await wallet.SendTransaction(tx);
        Debug.Log(hash);
        native_hash.text = "hash: " + hash;
    }

    // send erc20 token
    public async void SendErc20Token()
    {
        if (erc20_to == null || erc20_value == null) return;
        Debug.Log("send erc20 token");

        var config = wallet.GetWalletConfig();
        var unit = 18;
        var usdcAddress = "";
        if (config.chainType == WalletConfig.ChainType.polygon)
        {
            usdcAddress = "0x87F0E95E11a49f56b329A1c143Fb22430C07332a";
            unit = 6;
        }
        else if (config.chainType == WalletConfig.ChainType.bsc)
        {
            usdcAddress = "0x64544969ed7EBf5f083679233325356EbE738930";
            unit = 18;
        }
        else if (config.chainType == WalletConfig.ChainType.rangers)
        {
            usdcAddress = "0xd6ed1c13914ff1b08737b29de4039f542162cae1";
            unit = 6;
        }

        var transferFunction = new TransferFunction()
        {
            To = erc20_to,
            Amount = Web3.Convert.ToWei(BigDecimal.Parse(erc20_value), unit),
        };

        var signatureEncoder = new SignatureEncoder();
        var parameters = new Parameter[] { new Parameter("address", 1), new Parameter("uint256", 2) };
        var sha3Signature = signatureEncoder.GenerateSha3Signature("transfer", parameters, 4);
        var functionCallEncoder = new FunctionCallEncoder();
        var erc20Data = functionCallEncoder.EncodeRequest(transferFunction, sha3Signature);
        Debug.Log(erc20Data);


        var tx = new TransactionMessage(from: wallet.getAddress(), to: usdcAddress, value: "0x", data: erc20Data);
        var hash = await wallet.SendTransaction(tx);
        Debug.Log(hash);
        erc20_hash.text = "erc20_hash: " + hash;
    }

    public void CloseWeb()
    {
        Debug.Log("CloseWeb clicked");
        wallet.CloseWeb();
    }

    public void onWalletOpened()
    {
        Debug.Log("onWalletOpened");
    }

    public void onWalletClosed()
    {
        Debug.Log("onWalletClosed");
    }

    private void onDropdownChange(int value)
    {
        var config = wallet.GetWalletConfig();
        if (value == 0)
        {
            var rpcUrl = "https://node.wallet.unipass.id/polygon-mumbai";
            config.chainType = WalletConfig.ChainType.polygon;
            config.nodeRPC = rpcUrl;
            getUsdcBalance("0x87F0E95E11a49f56b329A1c143Fb22430C07332a", rpcUrl, 6);
        }
        else if (value == 1)
        {
            var rpcUrl = "https://node.wallet.unipass.id/bsc-testnet";
            config.chainType = WalletConfig.ChainType.bsc;
            config.nodeRPC = rpcUrl;
            getUsdcBalance("0x64544969ed7EBf5f083679233325356EbE738930", rpcUrl, 18);
        }
        else if (value == 2)
        {
            var rpcUrl = "https://node.wallet.unipass.id/rangers-robin";
            config.chainType = WalletConfig.ChainType.rangers;
            config.nodeRPC = rpcUrl;
            getUsdcBalance("0xd6ed1c13914ff1b08737b29de4039f542162cae1", rpcUrl, 6);
        }
        wallet.SetWalletConfig(wallet.GetWalletConfig());
    }

    private async void getUsdcBalance(string contractAddress, string rpcUrl, int deciaml)
    {
        if (!wallet.isConnected()) return;
        var balanceOfFunctionMessage = new BalanceOfFunction()
        {
            Owner = wallet.getAddress(),
        };

        var web3 = new Web3(rpcUrl);

        var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
        var balanceRaw = await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);
        var balance = Web3.Convert.FromWei(balanceRaw, deciaml);
        Debug.Log(balance);
        usdc_balance.text = "USDC Balance:" + balance.ToString();
    }

    public void copyAccount()
    {
        CopyToClipboard(account_text.text);
    }

    public void copySig()
    {
        CopyToClipboard(sig_text.text);
    }

    public void copyNativeHash()
    {
        CopyToClipboard(native_hash.text);
    }

    public void copyErc20Hash()
    {
        CopyToClipboard(erc20_hash.text);
    }

    private void CopyToClipboard(string str)
    {
        TextEditor textEditor = new TextEditor();
        textEditor.text = str;
        textEditor.SelectAll();
        textEditor.Copy();
    }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "_owner", 1)]
        public string Owner { get; set; }
    }

    [Function("transfer", "bool")]
    public class TransferFunction : FunctionMessage
    {
        [Parameter("address", "to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "amount", 2)]
        public BigInteger Amount { get; set; }
    }
}
