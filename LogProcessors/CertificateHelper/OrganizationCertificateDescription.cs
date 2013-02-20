using System.Security.Cryptography.X509Certificates;

namespace LogProcessors.CertificateHelper
{
	public class OrganizationCertificateDescription
	{

		public OrganizationCertificateDescription(X509Certificate certificate)
		{
			DistinguishedName distinguishedName = new DistinguishedName(certificate.Subject);
			subjectName = GetValue(distinguishedName, "CN");
			organizationName = GetValue(distinguishedName, "O");
			unstructuredName = GetValue(distinguishedName, "OID.1.2.840.113549.1.9.2");
			email = GetValue(distinguishedName, "E");
			owner = GetValue(distinguishedName, "SN");

			certificateContent = certificate.GetRawCertData();
            //ParsedOrganizationId parsedOrganizationId = ExtractParsedOrganizationId();
			//inn = parsedOrganizationId != null ? parsedOrganizationId.Inn : "";
			//kpp = parsedOrganizationId != null ? parsedOrganizationId.Kpp : "";
			//innfl = parsedOrganizationId != null ? parsedOrganizationId.Innfl : "";
		}


		private ParsedOrganizationId ExtractParsedOrganizationId()
		{
			if(unstructuredName == "")
				return null;
			// Следующие подмены нужны, чтобы система работала с сертификатами, выданными Атласом (С-Петербург): у них в сертификатах всегда присутствует два тире
			return new ParsedOrganizationId(unstructuredName.Replace("--", "-").TrimEnd('-'));
		}

		public string OrganizationName { get { return organizationName; } }
		public string UnstructuredName { get { return unstructuredName; } }
		//public string Inn { get { return inn; } }
		//public string Kpp { get { return kpp; } }
		//public string Innfl { get { return innfl; } }
		public string Owner { get { return owner; } }
		public string Email { get { return email; } }
	    public string SubjectName { get { return subjectName; } }

	    public byte[] Content { get { return certificateContent; } }

		internal static string GetValue(DistinguishedName distinguishedName, string name)
		{
			return distinguishedName.HasAttribute(name) ? distinguishedName[name] : "";
		}

		private readonly string organizationName;
		private readonly string unstructuredName;
		private readonly string inn;
		private readonly string kpp;
		private readonly string innfl;
		private readonly string owner;
		private readonly string email;
		private readonly byte[] certificateContent;
        private readonly string subjectName;

    }
}