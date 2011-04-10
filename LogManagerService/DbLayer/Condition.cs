using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace LogManagerService.DbLayer
{
    public class Condition
    {
        public Condition(string name, string value, ComparisonType comparisonType)
        {
            Name = name;
            Value = value;
            ComparisonType = comparisonType;
        }

        public string Name { get; private set; }
        public string Value { get; private set; }
        public ComparisonType ComparisonType { get; private set; }

        static List<string> _conditions = new List<string> { "datebegin", "dateend", "host", "ip", "inn", "sessionid" };

        public static List<Condition> Parse(NameValueCollection queryString)
        {
            List<Condition> result = new List<Condition>();
            foreach (var queryStringParameter in queryString.AllKeys)
            {
                if (!String.IsNullOrEmpty(queryString[queryStringParameter]) && _conditions.Contains(queryStringParameter.ToLower()))
                    result.Add(new Condition(queryStringParameter.ToLower(), queryString[queryStringParameter],
                                             queryString[queryStringParameter].Contains("*")
                                                 ? ComparisonType.Like
                                                 : ComparisonType.Strict));
            }
            return result;
        }
    }



}