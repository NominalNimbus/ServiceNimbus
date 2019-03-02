namespace Com.Lmax.Api.Account
{
    public class Account
    {
        private readonly long accountId;
        private readonly string accountName;

        public Account(long accountId, string accountName)
        {
            this.accountId = accountId;
            this.accountName = accountName;
        }

        public bool Equals(Account other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.accountId == accountId && Equals(other.accountName, accountName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Account)) return false;
            return Equals((Account) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (accountId.GetHashCode()*397) ^ (accountName != null ? accountName.GetHashCode() : 0);
            }
        }
    }
}