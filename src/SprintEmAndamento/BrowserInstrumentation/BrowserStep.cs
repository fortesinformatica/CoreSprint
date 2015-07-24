namespace CoreSprint.BrowserInstrumentation
{
    public class BrowserStep
    {
        public BrowserStep(string url, bool getData)
        {
            GetData = getData;
            Url = url;
        }

        public bool GetData { get; }

        public string Url { get; }
    }
}