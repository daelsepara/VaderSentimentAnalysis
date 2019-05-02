/* C# Implementation by Dael Separa (2019)
 * 
 * Based on the Open-source Python code implementation of C.J. Hutto and PHP sentiment analyzer code of David Oti
 * 
 * see: https://github.com/cjhutto/vaderSentiment
 * see: https://github.com/davmixcool/php-sentiment-analyzer
 * 
 * Comments from the original implementations are (mostly) preserved
 */
using System.Collections.Generic;

namespace VaderSentimentAnalysis
{
    public static class Constants
    {
        // (empirically derived mean sentiment intensity rating increase for booster words)
        public static readonly double B_INCR = 0.293;
        public static readonly double B_DECR = -0.293;

        // (empirically derived mean sentiment intensity rating increase for using
        // ALLCAPs to emphasize a word)
        public static readonly double C_INCR = 0.733;
        public static readonly double N_SCALAR = -0.74;

        public static readonly string[] NEGATE = {"aint", "arent", "cannot", "cant", "couldnt", "darent", "didnt", "doesnt",
            "ain't", "aren't", "can't", "couldn't", "daren't", "didn't", "doesn't",
            "dont", "hadnt", "hasnt", "havent", "isnt", "mightnt", "mustnt", "neither",
            "don't", "hadn't", "hasn't", "haven't", "isn't", "mightn't", "mustn't",
            "neednt", "needn't", "never", "none", "nope", "nor", "not", "nothing", "nowhere",
            "oughtnt", "shant", "shouldnt", "uhuh", "wasnt", "werent",
            "oughtn't", "shan't", "shouldn't", "uh-uh", "wasn't", "weren't",
            "without", "wont", "wouldnt", "won't", "wouldn't", "rarely", "seldom", "despite" };

        //booster/dampener 'intensifiers' or 'degree adverbs'
        //http://en.wiktionary.org/wiki/Category:English_degree_adverbs

        public static readonly Dictionary<string, double> BOOSTER_DICT = new Dictionary<string, double> {
            {"absolutely", B_INCR }, { "amazingly", B_INCR }, { "awfully", B_INCR }, { "completely", B_INCR },
            { "considerably", B_INCR }, { "decidedly", B_INCR }, { "deeply", B_INCR }, { "effing", B_INCR },
            { "enormously", B_INCR }, { "entirely", B_INCR }, { "especially", B_INCR }, { "exceptionally", B_INCR },
            { "extremely", B_INCR }, { "fabulously", B_INCR }, { "flipping", B_INCR }, { "flippin", B_INCR },
            { "fricking", B_INCR }, { "frickin", B_INCR }, { "frigging", B_INCR }, { "friggin", B_INCR },
            { "fully", B_INCR }, { "fucking", B_INCR }, { "greatly", B_INCR }, { "hella", B_INCR },
            { "highly", B_INCR }, { "hugely", B_INCR }, { "incredibly", B_INCR }, { "intensely", B_INCR },
            { "majorly", B_INCR }, { "more", B_INCR }, { "most", B_INCR }, { "particularly", B_INCR },
            { "purely", B_INCR }, { "quite", B_INCR }, { "really", B_INCR }, { "remarkably", B_INCR },
            { "so", B_INCR }, { "substantially", B_INCR }, { "thoroughly", B_INCR }, { "totally", B_INCR },
            { "tremendously", B_INCR }, { "uber", B_INCR }, { "unbelievably", B_INCR }, { "unusually", B_INCR },
            { "utterly", B_INCR }, { "very", B_INCR }, { "almost", B_DECR }, { "barely", B_DECR }, { "hardly", B_DECR }, { "just enough", B_DECR },
            { "kind of", B_DECR }, { "kinda", B_DECR }, { "kindof", B_DECR }, { "kind-of", B_DECR }, { "less", B_DECR }, { "little", B_DECR },
            { "marginally", B_DECR }, { "occasionally", B_DECR }, { "partly", B_DECR }, { "scarcely", B_DECR }, { "slightly", B_DECR },
            { "somewhat", B_DECR }, { "sort of", B_DECR }, { "sorta", B_DECR }, { "sortof", B_DECR }, { "sort-of", B_DECR }
        };

        // check for special case idioms using a sentiment-laden keyword known to SAGE
        public static readonly Dictionary<string, double> SPECIAL_CASE_IDIOMS = new Dictionary<string, double>{
            {"the shit", 3 }, {"the bomb", 3 }, {"bad ass", 1.5 }, {"yeah right", -2 },
            { "cut the mustard", 2 }, {"kiss of death", -1.5 }, {"hand to mouth", -2 }
        };

        public static double Normalize(double score, double alpha = 15.0)
        {
            var normalizedScore = score / System.Math.Sqrt((score * score) + alpha);

            if (normalizedScore < -1.0)
            {
                return -1.0;
            }

            if (normalizedScore > 1.0)
            {
                return 1.0;
            }

            return normalizedScore;
        }
    }
}
