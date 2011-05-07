using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace KeClientTracing.LogReading.LogDescribing
{
    public class LogDescriptor
    {
        public static string Describe (string method, string uri, SortedDictionary<string, string> queryString)
        {
            List<UriRule> uriRules = new List<UriRule>(DescriptionRules.GetDescriptionRulesSingletone().GetMatchingRules(uri));
            if (uriRules.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(uriRules[0].GetDescription(method));

            foreach (ParameterRule parameterRule in uriRules[0].GetMatchingParameters(queryString))
            {
                sb.AppendFormat(", {0}",
                                parameterRule.IncludeValue
                                    ? String.Format(parameterRule.Description, queryString[parameterRule.ParameterName])
                                    : parameterRule.Description);
            }
            return sb.ToString();
        }
    }
}