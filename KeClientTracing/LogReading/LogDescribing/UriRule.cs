using System;
using System.Collections.Generic;

namespace KeClientTracing.LogReading.LogDescribing
{
    public class UriRule
    {
        public UriRule(string uriPattern, string matchType, string getDescription, string postDescription)
        {
            _getDescription = getDescription;
            _postDescription = postDescription;
            UriPattern = uriPattern;
            MatchType = ParseMatchType(matchType);
        }

        MatchType ParseMatchType(string matchType)
        {
            switch (matchType.ToLower())
            {
                case "end": return MatchType.End;
                case "begin": return MatchType.Begin;
                case "contains": return MatchType.Contains;
            }
            return MatchType.Contains;
        }

        public void AddParameterRule(ParameterRule parameterRule)
        {
            _parameterRules.Add(parameterRule);
        }

        public bool IsMatched(string uri)
        {
            switch (MatchType)
            {
                case MatchType.End:
                    return uri.EndsWith(UriPattern,StringComparison.InvariantCultureIgnoreCase);
                case MatchType.Begin:
                    return uri.StartsWith(UriPattern,StringComparison.InvariantCultureIgnoreCase);
                default:
                    return uri.ToLowerInvariant().Contains(UriPattern.ToLowerInvariant());

            }
        }

        public IEnumerable<ParameterRule> GetMatchingParameters(SortedDictionary<string, string> queryParameters)
        {
            foreach (ParameterRule parameterRule in _parameterRules)
            {
                if (queryParameters.ContainsKey(parameterRule.ParameterName))
                    yield return parameterRule;
            }
        }


        List<ParameterRule> _parameterRules = new List<ParameterRule>();
        public string UriPattern { get; private set; }
        public MatchType MatchType { get; private set; }
        private string _getDescription;
        private string _postDescription;

        public string GetDescription(string method)
        {
            switch (method.ToLower())
            {
                case "post":
                    return _postDescription == "" ? _getDescription : _postDescription;
                default:
                    return _getDescription;
            }
        }
    }
}