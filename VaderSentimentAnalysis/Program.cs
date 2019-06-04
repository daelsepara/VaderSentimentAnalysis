using System;

namespace VaderSentimentAnalysis
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var sentimentText = "";
            var analyzer = new Analyzer("vader_lexicon.txt");

            foreach (var arg in args)
            {
                GetString(arg, "/TEXT=", ref sentimentText);

                sentimentText.Trim();

                if (!string.IsNullOrEmpty(sentimentText))
                {
                    var sentiment = analyzer.GetSentimentScore(sentimentText);

                    Console.WriteLine("\nSentiment text: {0}", sentimentText);
                    Console.WriteLine("Sentiment: ['neg' => {0:0.000}, 'neu' => {1:0.000}, 'pos' => {2:0.000}, 'compound' => {3:0.0000}]", sentiment.Item1, sentiment.Item2, sentiment.Item3, sentiment.Item4);

                    sentimentText = "";
                }
            }
        }

        static void GetString(string arg, string str, ref string dst)
        {
            if (arg.Length > 0 && str.Length > 0)
            {
                if (arg.Length > str.Length && string.Compare(arg.Substring(0, str.Length).ToUpper(), str, StringComparison.Ordinal) == 0)
                    dst = arg.Substring(str.Length);
            }
        }
    }
}
