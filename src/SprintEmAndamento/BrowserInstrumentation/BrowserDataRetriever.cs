using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DialectSoftware.Web.HtmlAgilityPack;
using mshtml;
using SHDocVw;

namespace CoreSprint.BrowserInstrumentation
{
    public class BrowserDataRetriever : IBrowserDataRetriever
    {
        private Func<HtmlDocument, string> _dataGetter;
        private readonly List<BrowserStep> _steps;
        private InternetExplorer _ie;
        private bool _browserStarted;
        private readonly AutoResetEvent _autoResetEvent;

        public BrowserDataRetriever(Func<HtmlDocument, string> dataGetter)
        {
            _dataGetter = dataGetter;
            _steps = new List<BrowserStep>();
            _autoResetEvent = new AutoResetEvent(false);
        }

        public IBrowserDataRetriever AddStep(string url, bool getData = true)
        {
            _steps.Add(new BrowserStep(url, getData));
            return this;
        }

        public string Retrieve()
        {
            var result = "";

            foreach (var browserStep in _steps)
            {
                if (!_browserStarted)
                {
                    _ie = LoadBrowser();
                    _ie.NavigateComplete2 += (object disp, ref object url) => { _autoResetEvent.Set(); };
                    _browserStarted = true;
                }
                if (TryGetData(browserStep, ref result))
                    break;
            }
            return result;
        }

        public void ClearSteps()
        {
            _steps.Clear();
            _autoResetEvent.Reset();
        }

        public IBrowserDataRetriever WithDataGetter(Func<HtmlDocument, string> dataGetter)
        {
            _dataGetter = dataGetter;
            return this;
        }

        private static InternetExplorer LoadBrowser()
        {
            Process.Start("iexplore", "about:Tabs");
            Thread.Sleep(1000);
            return GetInternetExplorerInstance();
        }

        private bool TryGetData(BrowserStep browserStep, ref string result)
        {
            if (!string.IsNullOrWhiteSpace(browserStep.Url))
                _ie.Navigate2(browserStep.Url);
            _autoResetEvent.Reset();
            Thread.Sleep(1000);
            _autoResetEvent.WaitOne();
            if (browserStep.GetData)
                result = _dataGetter(LoadDocument(_ie.Document as HTMLDocument));
            return !string.IsNullOrWhiteSpace(result);
        }

        private static HtmlDocument LoadDocument(HTMLDocument document)
        {
            var htmlDocument = new HtmlDocument();
            do
                htmlDocument.LoadHtml(document.documentElement.innerHTML);
            while (document.readyState != "complete");
            return htmlDocument;
        }

        private static InternetExplorer GetInternetExplorerInstance()
        {
            InternetExplorer ie;
            do
            {
                ShellWindows shellWindows = new ShellWindows(); ;
                ie = shellWindows.OfType<InternetExplorer>().FirstOrDefault(ieInstance => ieInstance.LocationURL == "about:Tabs");
            } while (ie == null);
            return ie;
        }

        public void Dispose()
        {
            _ie.Quit();
        }
    }
}