namespace UnipassWallet
{
    class UnipassResponse<T>
    {
        public string type;
        public T data;

        public UnipassResponse(string type, T data)
        {
            this.type = type;
            this.data = data;
        }
    }
}