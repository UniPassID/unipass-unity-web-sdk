using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Events;
using Vuplex.WebView;
using System.Linq;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System;
using System.Text;

namespace UnipassWallet
{
    public class Wallet : MonoBehaviour
    {

        private string UNIPASS_MESSSAGE_PREFIX = "\x18UniPass Signed Message:\n";

        public UnityEvent onReatyToConnect;

        public UnityEvent onWalletOpened;
        public UnityEvent onWalletClosed;

        [SerializeField] private WalletConfig walletConfig;

        [SerializeField] private bool enableRemoteDebugging;
        [SerializeField] private bool native2DMode;

        WebViewPrefab _mainWebViewPrefab;
        UnipassHardwareKeyboardListener _hardwareKeyboardListener;
        //UnipassHardwareKeyboardListener _hardwareKeyboardListener;

        private bool _walletVisible = false;
        private bool _messageListend = false;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        private Account? account;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        private string signMessage = "";
        private TransactionMessage transactionMessage;
        private IDictionary<string, TaskCompletionSource<string>> _callbackDict = new Dictionary<string, TaskCompletionSource<string>>();

        private void Awake()
        {
            if (enableRemoteDebugging)
            {
                Web.EnableRemoteDebugging();
            }
            Web.SetUserAgent(false);
            _UnipassDebugLog("Awake");

            //onWalletOpened.AddListener(() =>
            //{
            //    _walletWindow.Visible = true;
            //});

            //onWalletClosed.AddListener(() =>
            //{
            //    _walletWindow.Visible = false;
            //});
        }

        private void Start()
        {
            Debug.Log("Wallet Start123");
            _mainWebViewPrefab = WebViewPrefab.Instantiate(0.2f, 0.3f);
            _mainWebViewPrefab.transform.parent = transform;
            _mainWebViewPrefab.transform.localPosition = new Vector3(0, 1.1f, 0.4f);
            _mainWebViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
            _mainWebViewPrefab.NativeOnScreenKeyboardEnabled = true;
            
            _mainWebViewPrefab.Initialized += (initializedSender, initializedEventArgs) => {
                _mainWebViewPrefab.WebView.SetResolution(3200);
                _mainWebViewPrefab.ClickingEnabled = true;
            };
        }

        public async Task<Account> Connect(WalletConfig.ConnectType connectType, bool authorize)
        {
            _UnipassDebugLog("connect url: " + formatUnipassUrl("connect", connectType));
            _UnipassDebugLog("authorizel: " + authorize);
            walletConfig.authorize = authorize;
            _mainWebViewPrefab.WebView.LoadUrl(formatUnipassUrl("connect", connectType));
            _ShowWallet();
            var value = await ExecuteUnipassJS("connect");
            var _account = JsonConvert.DeserializeObject<UnipassResponse<Account>>(value).data;
            account = _account;
            return _account;
        }

        public async Task<string> SignMessage(string message)
        {
            _checkInitialized();
            signMessage = message;
            _UnipassDebugLog("sign-message url: " + formatUnipassUrl("sign-message"));
            _mainWebViewPrefab.WebView.LoadUrl(formatUnipassUrl("sign-message"));
            _ShowWallet();
            var value = await ExecuteUnipassJS("sign-message");
            return JsonConvert.DeserializeObject<UnipassResponse<string>>(value).data;
        }

        public async Task<string> SendTransaction(TransactionMessage transaction)
        {
            _checkInitialized();
            transactionMessage = transaction;
            _UnipassDebugLog("send-transaction url: " + formatUnipassUrl("send-transaction"));
            _mainWebViewPrefab.WebView.LoadUrl(formatUnipassUrl("send-transaction"));
            _ShowWallet();
            var value = await ExecuteUnipassJS("send-transaction");
            var hash = JsonConvert.DeserializeObject<UnipassResponse<string>>(value).data;
            return hash;
        }

        public static byte[] Combine(byte[] first, byte[] second, byte[] third)
        {
            byte[] ret = new byte[first.Length + second.Length + third.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
                             third.Length);
            return ret;
        }

        public async Task<bool> isValidSignature(string message, string sig)
        {
            _checkInitialized();
            string[] hexArray = new string[] { sig };

            byte[] bytes = Encoding.ASCII.GetBytes(message);

            byte[] messageBytes = Sha3Keccack.Current.CalculateHash(UNIPASS_MESSSAGE_PREFIX + bytes.Length.ToString() + message).HexToByteArray();

            byte[] sigOutput = string.Join("", hexArray.Select(x => x.RemoveHexPrefix()).ToArray()).HexToByteArray();
            var web3 = new Web3(walletConfig.nodeRPC);
            var isValidSignatureFunctionMessage = new IsValidSignatureFunction()
            {
                Hash = messageBytes,
                Signature = sigOutput,
            };
            var sigHandler = web3.Eth.GetContractQueryHandler<IsValidSignatureFunction>();
            var code = await sigHandler.QueryAsync<byte[]>(account.address, isValidSignatureFunctionMessage);
            Debug.Log(code.ToHex() == "1626ba7e");
            return code.ToHex() == "1626ba7e";
        }

        public string getAddress()
        {
            _checkInitialized();
            return account.address;
        }

        public void CloseWeb()
        {
            _HideWallet();
            signMessage = "";
            _callbackDict.Clear();
        }

        public void SetWalletConfig(WalletConfig config)
        {
            walletConfig = config;
        }

        public WalletConfig GetWalletConfig()
        {
            return walletConfig;
        }

        public bool isConnected()
        {
            return account != null;
        }

        private void addMessageListener()
        {
            if (_messageListend) return;
            _mainWebViewPrefab.WebView.MessageEmitted += (sender, eventArgs) =>
            {
                if (eventArgs.Value.Contains("invalid login"))
                {
                    _UnipassDebugLog("invalid login");
                    _HideWallet();
                    account = null;
                    signMessage = "";
                    _callbackDict.Clear();
                }
                else if (eventArgs.Value.Contains("DECLINE"))
                {
                    _HideWallet();
                    signMessage = "";
                    _callbackDict.Clear();
                }
                else if (eventArgs.Value == "onConnectReady")
                {
                    _UnipassDebugLog("onConnectReady");
                    onConnectPageReady();
                }
                else if (eventArgs.Value == "onSignMessageReady")
                {
                    _UnipassDebugLog("onSignMessageReady");
                    onSignMessageReady();
                }
                else if (eventArgs.Value == "onSendTransactionReady")
                {
                    _UnipassDebugLog("onSendTransactionReady");
                    onSendTransactionReady();
                }
                else if (eventArgs.Value.Contains("UP_RESPONSE_CONNECT"))
                {
                    _UnipassDebugLog("UP_RESPONSE_CONNECT");
                    if (_callbackDict.Count == 0) return;
                    _callbackDict["connect"].TrySetResult(eventArgs.Value);
                    _callbackDict.Remove("connect");
                    _HideWallet();
                }
                else if (eventArgs.Value.Contains("UP_RESPONSE_SIGN"))
                {
                    _UnipassDebugLog("UP_RESPONSE_SIGN");
                    if (_callbackDict.Count == 0) return;
                    _callbackDict["sign-message"].TrySetResult(eventArgs.Value);
                    _callbackDict.Remove("sign-message");
                    _HideWallet();
                }
                else if (eventArgs.Value.Contains("UP_RESPONSE_TRANSACTION"))
                {
                    _UnipassDebugLog("UP_RESPONSE_TRANSACTION");
                    if (_callbackDict.Count == 0) return;
                    _callbackDict["send-transaction"].TrySetResult(eventArgs.Value);
                    _callbackDict.Remove("send-transaction");
                    _HideWallet();
                }
                else if (eventArgs.Value == "initialized")
                {
                    _UnipassDebugLog("Wallet Initialized1212!");
                }
            };
            _mainWebViewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
            };
            _messageListend = true;
        }

        private void onConnectPageReady()
        {
            _UnipassDebugLog("onConnectPageReady");
            var js = @"
                window.onConnectPageReady({
                    type: 'UP_LOGIN',
                    appSetting: {
                        appName: '" + walletConfig.appName + @"',
                        appIcon: '" + walletConfig.appIcon + @"',
                        chain: '" + walletConfig.chainType.ToString() + @"',
                        theme: '" + walletConfig.theme.ToString() + @"',
                    },
                      payload: {
                        returnEmail: '" + walletConfig.returnEmail + @"',
                        authorize: '" + walletConfig.authorize + @"',
                      }
                });
            ";
            _UnipassDebugLog(js);
            _mainWebViewPrefab.WebView.ExecuteJavaScript(js);
        }

        private void onSignMessageReady()
        {
            if (signMessage.Length == 0) return;
            var js = @"
                window.onSignMessagePageReady({
                    type: 'UP_SIGN_MESSAGE',
                    appSetting: {
                        appName: '" + walletConfig.appName + @"',
                        appIcon: '" + walletConfig.appIcon + @"',
                        chain: '" + walletConfig.chainType.ToString() + @"',
                        theme: '" + walletConfig.theme.ToString() + @"',
                    },
                    payload: {
                        from: '" + account.address + @"',
                        msg: '" + signMessage + @"',
                    }
                 });
            ";
            _UnipassDebugLog(js);
            _mainWebViewPrefab.WebView.ExecuteJavaScript(js);
        }

        private void onSendTransactionReady()
        {
            if (transactionMessage == null) return;
            var js = @"
                window.onSendTransactionPageReady({
                    type: 'UP_TRANSACTION',
                    appSetting: {
                        appName: '" + walletConfig.appName + @"',
                        appIcon: '" + walletConfig.appIcon + @"',
                        chain: '" + walletConfig.chainType.ToString() + @"',
                        theme: '" + walletConfig.theme.ToString() + @"',
                    },
                    payload: {
                        from: '" + transactionMessage.from + @"',
                        to: '" + transactionMessage.to + @"',
                        value: '" + transactionMessage.value + @"',
                        data: '" + transactionMessage.data + @"',
                    }
                 });
            ";
            _UnipassDebugLog(js);
            _mainWebViewPrefab.WebView.ExecuteJavaScript(js);
        }

        private Task<string> ExecuteUnipassJS(string route)
        {
            var jsPromiseResolved = new TaskCompletionSource<string>();
            _callbackDict.Add(route, jsPromiseResolved);
            return jsPromiseResolved.Task;
        }

        private void _UnipassDebugLog(string message)
        {
            Debug.Log("[Unipass] " + message);
        }

        private void _ShowWallet()
        {
            addMessageListener();
            if (!_walletVisible)
            {
                onWalletOpened.Invoke();
                _mainWebViewPrefab.Visible = true;
                _walletVisible = true;
            }
        }

        private void _HideWallet()
        {
            if (_walletVisible)
            {
                onWalletClosed.Invoke();
                _mainWebViewPrefab.WebView.LoadHtml("<style>*{background:trasnprent;}</style>");
                _mainWebViewPrefab.Visible = false;
                _walletVisible = false;
            }
        }

        private void _setUpKeyboards()
        {

            // Send keys from the hardware (USB or Bluetooth) keyboard to the webview.
            // Use separate `KeyDown()` and `KeyUp()` methods if the webview supports
            // it, otherwise just use `IWebView.HandleKeyboardInput()`.
            // https://developer.vuplex.com/webview/IWithKeyDownAndUp
            _hardwareKeyboardListener = UnipassHardwareKeyboardListener.Instantiate();
            _hardwareKeyboardListener.KeyDownReceived += (sender, eventArgs) => {
                var webViewWithKeyDown = _mainWebViewPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyDown == null)
                {
                    _mainWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
                }
                else
                {
                    webViewWithKeyDown.KeyDown(eventArgs.Value, eventArgs.Modifiers);
                }
            };
            _hardwareKeyboardListener.KeyUpReceived += (sender, eventArgs) => {
                var webViewWithKeyUp = _mainWebViewPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyUp != null)
                {
                    webViewWithKeyUp.KeyUp(eventArgs.Value, eventArgs.Modifiers);
                }
            };

            // Also add an on-screen keyboard under the main webview.
            var keyboard = Keyboard.Instantiate();
            keyboard.transform.SetParent(_mainWebViewPrefab.transform, false);
            keyboard.transform.localPosition = new Vector3(0, -0.31f, 0);
            keyboard.transform.localEulerAngles = Vector3.zero;
            keyboard.InputReceived += (sender, eventArgs) => {
                _mainWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
            };
        }

        private string formatUnipassUrl(string type, WalletConfig.ConnectType connectType = WalletConfig.ConnectType.both)
        {
            string _domain = walletConfig.domain.EndsWith('/') ? walletConfig.domain : walletConfig.domain + "/";
            string to = "";
            if (connectType == WalletConfig.ConnectType.google)
            {
                to = "?connectType=google";
            }
            if (connectType == WalletConfig.ConnectType.email)
            {
                to = "?connectType=email";
            }
            return walletConfig.protocol + "://" + _domain + type + to;
        }

        private void _checkInitialized()
        {
            if (account == null)
            {
                throw new System.Exception("UniPass SDK is not initialized, please connect first");
            }
        }

        [Function("isValidSignature", "bytes4")]
        public class IsValidSignatureFunction : FunctionMessage
        {
            [Parameter("bytes32", "_hash", 1)]
            public byte[] Hash { get; set; }

            [Parameter("bytes", "_signature", 2)]
            public byte[] Signature { get; set; }
        }
    }
}
