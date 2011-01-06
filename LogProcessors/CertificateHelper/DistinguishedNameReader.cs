using System;
using System.Text;

namespace LogProcessors.CertificateHelper
{
	internal class DistinguishedNameReader
	{
		public DistinguishedNameReader(string s)
		{
			this.s = s;
		}

		public void Next()
		{
			++position;
		}

		public string ReadUntil(params char[] stopChars)
		{
			int startPos = position;
			while(!BeyondEnd)
			{
				if(Array.IndexOf(stopChars, CurChar) != -1) break;
				if(CurChar == '"') throw new FormatException("Неверный формат: присутствие символа кавычки не допустимо");
				Next();
			}
			return s.Substring(startPos, position - startPos);
		}

		public string ReadQuotedTextUntil(params char[] stopChars)
		{
			if(BeyondEnd) return String.Empty;
			if(CurChar != '"') return ReadUntil(AddChars(stopChars, '"'));
			Next();
			StringBuilder result = new StringBuilder();
			bool quotation = false;
			while(!BeyondEnd)
			{
				if(!quotation)
				{
					if(CurChar != '"') result.Append(CurChar);
				}
				else
				{
					if(CurChar != '"') break;
					result.Append('"');
				}
				if(CurChar == '"') quotation = !quotation;
				Next();
			}
			if(!quotation) throw new FormatException("Отсутствует закрывающая кавычка");
			if(!BeyondEnd && Array.IndexOf(stopChars, CurChar) == -1)
				throw new FormatException("Неверный формат текста в кавычках");
			return result.ToString();
		}

		public bool BeyondEnd { get { return position >= s.Length; } }
		public bool IsLastChar { get { return position == s.Length - 1; } }

		public char CurChar
		{
			get
			{
				if(position < 0 || position >= s.Length)
					throw new ArgumentOutOfRangeException(String.Format("Значение {0}. Должно быть больше 0 и меньше {1}", position, s.Length));
				return s[position];
			}
		}

		public int Position { get { return position; } }

		private static char[] AddChars(char[] chars, params char[] newChars)
		{
			char[] result = new char[chars.Length + newChars.Length];
			chars.CopyTo(result, 0);
			newChars.CopyTo(result, chars.Length);
			return result;
		}

		private int position;

		private readonly string s;
	}
}