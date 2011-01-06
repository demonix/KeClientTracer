using System;

namespace LogProcessors.TokenHelper
{
    public class Token
    {
        public Token(Guid abon, Guid user, string thumbprint, DateTime validTo)
        {
            this.abon = abon;
            this.user = user;
            this.thumbprint = thumbprint;
            this.validTo = validTo;
        }

        public override string ToString()
        {
            return string.Format("AbonentId: '{0}', UserId: '{1}', Thumbprint: '{2}', ValidTo: '{3}'", Abon, User, Thumbprint, ValidTo.ToString("dd.MM.yyyy HH:mm:ss"));
        }

        public static Token Deserialize(string input)
        {
            string[] splits = input.Split('\n');
            if (splits.Length != 4)
                throw new Exception(string.Format("Can't parse token from string: {0}.", input));
            Guid abon = new Guid(splits[0]);
            Guid user = new Guid(splits[1]);
            string thumbprint = splits[2];
            long ticks;
            if (!long.TryParse(splits[3], out ticks))
                throw new Exception(string.Format("Can't parse token from string: {0}.", input));
            DateTime validTo = new DateTime(ticks);
            return new Token(abon, user, thumbprint, validTo);
        }

        public Guid Abon { get { return abon; } }
        public Guid User { get { return user; } }
        public string Thumbprint { get { return thumbprint; } }
        public DateTime ValidTo { get { return validTo; } }

        private readonly Guid abon;
        private readonly Guid user;
        private readonly string thumbprint;
        private DateTime validTo;
    }
}
