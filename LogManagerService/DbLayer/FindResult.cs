using System.Collections.Generic;
using System.Text;
using LogManagerService.Handlers;

namespace LogManagerService.DbLayer
{
    public class FindResult
    {
        List<FindResultEntry> _findResults= new List<FindResultEntry>();

        public FindResult()
        { }
        public FindResult(List<FindResultEntry> resultList)
        {
            _findResults = resultList;
        }

        public int Count()
        {
            return _findResults.Count;
        }

        public void Add (FindResultEntry findResultEntry)
        {
            _findResults.Add(findResultEntry);
        }
        public string ToSimpleHtml()
        {
            StringBuilder result = new StringBuilder();
            result.Append("<html><head><meta http-equiv=\"content-type\" content=\"text/html;charset=UTF-8\"/></head><table><tr>");
            result.Append("<td>Id</td>");
            result.Append("<td>Date</td>");
            result.Append("<td>Host</td>");
            result.Append("<td>Ip</td>");
            result.Append("<td>INN</td>");
            result.Append("<td>Session Id</td>");
            result.Append("<td>Session Start</td>");
            result.Append("<td>Session End</td>");
            foreach (FindResultEntry findResultEntry in _findResults)
            {
                result.Append(findResultEntry.ToSimpleHtml());
            }
            result.Append("</tr></table></html>");
            return result.ToString();
        }
    }
}