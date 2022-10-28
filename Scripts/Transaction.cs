namespace UnipassWallet
{
    public class TransactionMessage
    {
        public string from;
        public string to;
        public string value;
        public string data;

        public TransactionMessage(string from, string to, string value, string data)
        {
            this.from = from;
            this.to = to;
            this.value = value;
            this.data = data;
        }
    }
}
