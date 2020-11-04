namespace ProSuite.Commons.Cryptography
{
	public class KeyPair
	{
		public KeyPair(string privateKey, string publicKey)
		{
			PrivateKey = privateKey;
			PublicKey = publicKey;
		}

		public string PrivateKey { get; }

		public string PublicKey { get; }
	}
}
