namespace UnipassWallet
{
    public class Account
    {
        public string address;
        public string email;
        public bool newborn;

        public Account(string address, string email, bool newborn)
        {
            this.address = address;
            this.email = email;
            this.newborn = newborn;
        }
    }
}