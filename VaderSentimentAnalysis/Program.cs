using System;

namespace VaderSentimentAnalysis
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var sentimentText = "The movie was good.";

            var analyzer = new Analyzer("vader_lexicon.txt");

            var sentiment = analyzer.GetSentimentScore(sentimentText);

            Console.WriteLine("Sentiment text: {0}", sentimentText);
            Console.WriteLine("Sentiment: ['neg' => {0:0.000}, 'neu' => {1:0.000}, 'pos' => {2:0.000}, 'compound' => {3:0.0000}]", sentiment.Item1, sentiment.Item2, sentiment.Item3, sentiment.Item4);
        }
    }
}
