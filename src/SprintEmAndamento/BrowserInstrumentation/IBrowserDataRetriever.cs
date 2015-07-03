using System;
using DialectSoftware.Web.HtmlAgilityPack;

namespace CoreSprint.BrowserInstrumentation
{
    public interface IBrowserDataRetriever : IDisposable
    {
        IBrowserDataRetriever AddStep(string url, bool getData = true);
        string Retrieve();
        void ClearSteps();
        IBrowserDataRetriever WithDataGetter(Func<HtmlDocument, string> dataGetter);
    }
}