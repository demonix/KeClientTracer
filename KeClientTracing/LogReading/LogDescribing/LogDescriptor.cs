using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace KeClientTracing.LogReading.LogDescribing
{
    public class LogDescriptor
    {
        public static string Describe (string method, string uri, string queryString)
        {
            List<UriRule> uriRules = new List<UriRule>(DescriptionRules.GetDescriptionRulesSingletone().GetMatchingRules(uri));
            if (uriRules.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(uriRules[0].GetDescription(method));

            SortedDictionary<string, string> qs = QueryStringToDictionary(queryString);
            foreach (ParameterRule parameterRule in uriRules[0].GetMatchingParameters(qs))
            {
                sb.AppendFormat(", {0}",
                                parameterRule.IncludeValue
                                    ? String.Format(parameterRule.Description, qs[parameterRule.ParameterName])
                                    : parameterRule.Description);
            }
            return sb.ToString();
        }
        static SortedDictionary<string, string> QueryStringToDictionary(string qs)
        {
            SortedDictionary<string, string> result = new SortedDictionary<string, string>();
            qs = HttpUtility.UrlDecode(qs);
            string[] qp = qs.Split('&');
            foreach (string s in qp)
            {
                string[] pv = s.Split(new[] {'='}, 2);
                if(pv.Length<2) continue;

                if (result.ContainsKey(pv[0]))
                    result[pv[0]] = pv[1];
                else
                    result.Add(pv[0],pv[1]);
            }
            return result;
        }
    }
}