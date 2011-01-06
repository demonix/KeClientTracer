using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LogProcessors.CertificateHelper
{
	internal class ParsedOrganizationId
	{
		public ParsedOrganizationId(string organizationId)
		{
			this.organizationId = organizationId;
			Match match = organizationIdRegex.Match(organizationId);
			if(!match.Success)
				throw new Exception(string.Format("'{0}' не является корректным идентификатором организации", organizationId));
			inn = match.Groups["inn"].Value;
			kpp = match.Groups["kpp"].Value;
			innfl = match.Groups["innfl"].Value;
			mriCode = match.Groups["mri"].Value;
		}

		public ParsedOrganizationId(string inn, string kpp, string innfl)
		{
			this.inn = inn;
			this.kpp = kpp;
			this.innfl = innfl;
			StringBuilder sb = new StringBuilder();
			sb.Append(inn);
			if(kpp != "")
			{
				sb.Append('-');
				sb.Append(kpp);
			}
			if(innfl != "")
			{
				sb.Append('-');
				sb.Append(innfl);
			}
			organizationId = sb.ToString();
		}

		public string OrganizationId { get { return organizationId; } }
		public string Inn { get { return inn; } }
		public string Kpp { get { return kpp; } }
		public string Innfl { get { return innfl; } }
		public string MriCode { get { return mriCode; } }
		public bool ContainsKpp { get { return kpp != ""; } }
		public bool ContainsInnfl { get { return innfl != ""; } }
		public bool IsMriOrganizationId { get { return mriCode != ""; } }

		public override string ToString()
		{
			return organizationId;
		}

		public bool EqualsExactly(ParsedOrganizationId that)
		{
			return organizationId == that.organizationId;
		}

		public bool EqualsInnAndKpp(ParsedOrganizationId that)
		{
			return inn == that.inn && kpp == that.kpp;
		}

		public static bool IsValidOrganizationId(string organizationId)
		{
			return organizationIdRegex.IsMatch(organizationId);
		}

		public static bool IsValidInn(string inn)
		{
			return innRegex.IsMatch(inn);
		}

		public static bool IsValidKpp(string kpp)
		{
			return kppRegex.IsMatch(kpp);
		}

		public static bool IsValidInnfl(string innfl)
		{
			return innflRegex.IsMatch(innfl);
		}

		public static string CreateMriOrganizationId(string innKpp, string mriCode)
		{
			return string.Format("{0}_MRI{1}", innKpp, mriCode);
		}

		private readonly string organizationId;
		private readonly string inn;
		private readonly string kpp;
		private readonly string innfl;
		private readonly string mriCode;

		private static readonly Regex organizationIdRegex = new Regex(@"^(?<inn>\d{10}|\d{12}?)(?:-(?<kpp>\d{9}?))?(?:-(?<innfl>\d{12}?))?(?:_MRI(?<mri>\d{4}?))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex innRegex = new Regex(@"^(?:\d{10}|\d{12})$", RegexOptions.Compiled);
		private static readonly Regex kppRegex = new Regex(@"^\d{9}$", RegexOptions.Compiled);
		private static readonly Regex innflRegex = new Regex(@"^\d{12}$", RegexOptions.Compiled);
	}
}