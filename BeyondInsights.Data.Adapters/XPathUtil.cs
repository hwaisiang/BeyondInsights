using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeyondInsights.Data.Adapters
{
    public static class XPathUtil
    {
        public static HtmlNode GetParentNode(HtmlDocument doc, string nodeName, string filterString)
        {

            var node = doc.DocumentNode.SelectNodes("//" + nodeName);

            var selectedSingleNode = node.SingleOrDefault((n) =>
            {
                return n.InnerText.Contains(filterString);
            });

            return selectedSingleNode.ParentNode;
        }
        public static HtmlNode GetNode(HtmlDocument doc, string nodeName, string filterString)
        {

            var node = doc.DocumentNode.SelectNodes("//" + nodeName);

            var selectedSingleNode = node.SingleOrDefault((n) =>
            {
                return n.InnerText.Contains(filterString);
            });

            return selectedSingleNode;
        }
        public static HtmlNode GetParentNodeByAttribute(HtmlDocument doc, string nodeName,string attributeName, string value)
        {

            var node = doc.DocumentNode.SelectSingleNode("//" + nodeName + string.Format("[@{0}=\"{1}\"]", attributeName, value));

            return node.ParentNode;
        }
    }
}
