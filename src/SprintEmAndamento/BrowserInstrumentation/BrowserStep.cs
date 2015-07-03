namespace CoreSprint.BrowserInstrumentation
{
    public class BrowserStep
    {
        private readonly bool _getData;
        private readonly string _url;

        public BrowserStep(string url, bool getData)
        {
            _getData = getData;
            _url = url;
        }

        public bool GetData
        {
            get { return _getData; }
        }

        public string Url
        {
            get { return _url; }
        }
    }
}