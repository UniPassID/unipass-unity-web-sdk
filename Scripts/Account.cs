namespace UnipassWallet
{
    public class Account
    {
        public string address;
        public string email;
        public bool newborn;
        public string message;
        public string signature;

        public Account(string address, string email, bool newborn, string message, string signature)
        {
            this.address = address;
            this.email = email;
            this.newborn = newborn;
            this.message = message;
            this.signature = signature;
        }
    }
}