
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace KeClientTracing.LogReading.LogDescribing
{
    public class DescriptionRules
    {
        List<UriRule> _uriRules = new List<UriRule>();
        const string DescriptionsSettingsPath = "logDescriptions.xml";
        private static DescriptionRules _dr;
        private static object _locker = new object();
        private static FileSystemWatcher _fsw;

        public static DescriptionRules GetDescriptionRulesSingletone()
        {
            if (_dr == null)
                lock (_locker)
                {
                    if (_dr == null)
                    {
                        _dr = DescriptionRules.Load();
                        _fsw = new FileSystemWatcher(".",DescriptionsSettingsPath);
                        _fsw.Changed += DescriptionsSettingsChanged;
                        _fsw.EnableRaisingEvents = true;
                    }
                }
            return _dr;
        }

        private static void DescriptionsSettingsChanged(object state, FileSystemEventArgs fileSystemEventArgs)
        {
            Console.WriteLine("DescriptionsSettingsChanged! Reread...");
            lock (_locker)
            {
                _dr = DescriptionRules.Load();
            }
        }

        public static DescriptionRules Load()
        {
            DescriptionRules descRules = new DescriptionRules();
            XDocument xRulesDoc = XDocument.Load(new StreamReader(DescriptionsSettingsPath));
            var xUriRules = xRulesDoc.Root.Elements("rule");
            foreach (XElement xUriRule in xUriRules)
            {

                var xUriRuleUriAttribute = xUriRule.Attribute("uri");
                var xUriRuleMatchTypeAttribute = xUriRule.Attribute("uriMatchType");
                var xUriRuleGetDescAttribute = xUriRule.Attribute("getDesc");
                var xUriRulePostDescAttribute = xUriRule.Attribute("postDesc");
                if (xUriRuleUriAttribute == null || xUriRuleUriAttribute.Value == "")
                    continue;

                UriRule uriRule = new UriRule(
                    xUriRuleUriAttribute.Value,
                    xUriRuleMatchTypeAttribute == null ? "" : xUriRuleMatchTypeAttribute.Value,
                    xUriRuleGetDescAttribute == null ? "" : xUriRuleGetDescAttribute.Value,
                    xUriRulePostDescAttribute == null ? "" : xUriRulePostDescAttribute.Value);
                var xParameterRules = xUriRule.Elements("parameter");
                foreach (XElement xParameterRule in xParameterRules)
                {
                    var xParameterRuleNameAttribute = xParameterRule.Attribute("name");
                    var xParameterRuleDescAttribute = xParameterRule.Attribute("description");
                    var xParameterRuleIncludeValueAttribute = xParameterRule.Attribute("includeValue");
                    if (xParameterRuleNameAttribute == null || xParameterRuleNameAttribute.Value == "")
                        continue;

                    ParameterRule parameterRule = new ParameterRule(
                        xParameterRuleNameAttribute.Value,
                        xParameterRuleDescAttribute == null ? "" : xParameterRuleDescAttribute.Value,
                        xParameterRuleIncludeValueAttribute == null ? false : Convert.ToBoolean(xParameterRuleIncludeValueAttribute.Value));
                    uriRule.AddParameterRule(parameterRule);
                }
                descRules._uriRules.Add(uriRule);
            }
            return descRules;

        }
        
        public IEnumerable<UriRule> GetMatchingRules(string uri)
        {
            foreach (UriRule uriRule in _uriRules)
            {
                if (uriRule.IsMatched(uri)) yield return uriRule;
            }
        }
    }
}