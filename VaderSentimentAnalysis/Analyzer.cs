/* C# Implementation by Dael Separa (2019)
 * 
 * Based on the Open-source Python code implementation of C.J. Hutto and PHP sentiment analyzer code of David Oti
 * 
 * see: https://github.com/cjhutto/vaderSentiment
 * see: https://github.com/davmixcool/php-sentiment-analyzer
 * 
 * Comments from the original implementations are (mostly) preserved
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VaderSentimentAnalysis
{
    /**
     * Sentiment analyzer   
     */   
    public class Analyzer
    {
        private readonly string LexiconFile;
        private Dictionary<string, double> Lexicon;
        private SentimentText CurrentSentimentText;

        public Analyzer(string file = "vader_lexicon.txt")
        {
            LexiconFile = file;
            Lexicon = MakeLexiconDictionary(LexiconFile);
        }

        public CultureInfo ci = new CultureInfo("en-US");

        // Convert lexicon file to a dictionary
        public Dictionary<string, double> MakeLexiconDictionary(string lexicon_file)
        {
            var lex_dict = new Dictionary<string, double>();

            if (File.Exists(lexicon_file))
            {
                var lines = File.ReadAllLines(lexicon_file);

                if (lines.Length > 0)
                {
                    foreach (var line in lines)
                    {
                        var tokens = line.Trim().Split('\t');

                        if (tokens.GetLength(0) >= 2)
                        {
                            var word = tokens[0];
                            var measure = Convert.ToDouble(tokens[1], ci);

                            if (!lex_dict.ContainsKey(word))
                                lex_dict.Add(word, measure);
                        }
                    }
                }
            }

            return lex_dict;
        }

        public Tuple<double, double, double, double> GetSentimentScore(string text)
        {
            return PolarityScores(text);
        }

        /*
         * Return a Tuple for sentiment strength based on the input text.
         * Positive values are positive valence, negative value are negative
         * valence.
         *
         */
        public Tuple<double, double, double, double> PolarityScores(string text)
        {
            // TODO: convert emojis to their textual descriptions and add to text

            CurrentSentimentText = new SentimentText(text);

            var sentiments = new List<double>();

            var words_and_emoticons = CurrentSentimentText.WordsAndEmoticons;

            for (var i = 0; i < words_and_emoticons.GetLength(0); i++)
            {
                var valence = 0.0;

                var item = words_and_emoticons[i];

                // check for vader_lexicon words that may be used as modifiers or negations
                if (Constants.BOOSTER_DICT.ContainsKey(item.ToLower()))
                {
                    sentiments.Add(valence);

                    continue;
                }

                if (i < words_and_emoticons.GetLength(0) - 1 && item.ToLower().Equals("kind") && words_and_emoticons[i + 1].ToLower().Equals("of"))
                {
                    sentiments.Add(valence);

                    continue;
                }

                sentiments = SentimentValence(valence, item, i, sentiments.ToArray()).ToList();
            }

            return ScoreValence(ButCheck(words_and_emoticons, sentiments.ToArray()), text);
        }

        public Tuple<double, double, double, double> ScoreValence(double[] sentiments, string text)
        {
            var compound = 0.0;
            var pos = 0.0;
            var neg = 0.0;
            var neu = 0.0;

            if (sentiments.GetLength(0) > 0)
            {
                var sum_s = sentiments.Sum();

                // compute and add emphasis from punctuation in text
                var punct_emph_amplifier = _punctuation_emphasis(text);

                if (sum_s > 0)
                {
                    sum_s += punct_emph_amplifier;
                }
                else if (sum_s < 0)
                {
                    sum_s -= punct_emph_amplifier;
                }

                compound = Constants.Normalize(sum_s);

                // discriminate between positive, negative and neutral sentiment scores
                var scores = _sift_sentiment_scores(sentiments);
                var pos_sum = scores.Item1;
                var neg_sum = scores.Item2;
                var neu_count = scores.Item3;

                if (pos_sum > Math.Abs(neg_sum))
                {
                    pos_sum += punct_emph_amplifier;
                }
                else if (pos_sum < Math.Abs(neg_sum))
                {
                    neg_sum -= punct_emph_amplifier;
                }

                var total = pos_sum + Math.Abs(neg_sum) + neu_count;

                pos = Math.Abs(pos_sum / total);
                neg = Math.Abs(neg_sum / total);
                neu = Math.Abs(neu_count / total);
            }

            return new Tuple<double, double, double, double>(Math.Round(neg, 3), Math.Round(neu, 3), Math.Round(pos, 3), Math.Round(compound, 4));
        }

        private double[] SentimentValence(double valence, string item, int i, double[] sentiments)
        {
            var sentimentList = sentiments.ToList();

            var adjustedValence = valence;

            var IsCapDifferential = CurrentSentimentText.IsAllCapsDifferential;
            var words_and_emoticons = CurrentSentimentText.WordsAndEmoticons;
            var item_lowercase = item.ToLower();

            if (Lexicon.ContainsKey(item_lowercase))
            {
                // get the sentiment valence
                adjustedValence = Lexicon[item_lowercase];

                // check if sentiment laden word is in ALL CAPS (while others aren't)
                if (item.All(char.IsUpper) && IsCapDifferential)
                {
                    if (adjustedValence > 0)
                    {
                        adjustedValence += Constants.C_INCR;
                    }
                    else
                    {
                        adjustedValence -= Constants.C_INCR;
                    }
                }

                for (var start_i = 0; start_i < 3; start_i++)
                {
                    // dampen the scalar modifier of preceding words and emoticons
                    // (excluding the ones that immediately preceed the item) based
                    // on their distance from the current item.
                    if (i > start_i && !Lexicon.ContainsKey(words_and_emoticons[i - (start_i + 1)].ToLower()))
                    {
                        var s = ScalarIncDec(words_and_emoticons[i - (start_i + 1)], adjustedValence, IsCapDifferential);

                        if (start_i == 1 && Math.Abs(s) > double.Epsilon)
                        {
                            s = s * 0.95;
                        }

                        if (start_i == 2 && Math.Abs(s) > double.Epsilon)
                        {
                            s = s * 0.9;
                        }

                        adjustedValence += s;

                        adjustedValence = NegationCheck(adjustedValence, words_and_emoticons, start_i, i);

                        if (start_i == 2)
                        {
                            adjustedValence = SpecialIdiomsCheck(adjustedValence, words_and_emoticons, i);
                        }
                    }
                }

                adjustedValence = LeastCheck(adjustedValence, words_and_emoticons, i);
            }

            sentimentList.Add(adjustedValence);

            return sentimentList.ToArray();
        }

        private double NegationCheck(double valence, string[] words_and_emoticons, int start_i, int i)
        {
            var adjustedValence = valence;

            var words_and_emoticons_lower = words_and_emoticons.Select(s => s.ToLowerInvariant()).ToArray();

            if (start_i == 0)
            {
                if (IsNegated(new string[] { words_and_emoticons_lower[i - (start_i + 1)] }))
                {
                    adjustedValence *= Constants.N_SCALAR;
                }
            }

            if (start_i == 1)
            {
                if (words_and_emoticons_lower[i - 2].Equals("never") &&
                    (words_and_emoticons_lower[i - 1].Equals("so") ||
                    words_and_emoticons_lower[i - 1].Equals("this"))
                )
                {
                    adjustedValence *= 1.25;
                }
                else if (words_and_emoticons_lower[i - 2].Equals("without") &&
                    words_and_emoticons_lower[i - 1].Equals("doubt")
                )
                {

                }
                else if (IsNegated(new string[] { words_and_emoticons_lower[i - (start_i + 1)] }))
                {
                    adjustedValence *= Constants.N_SCALAR;
                }
            }

            if (start_i == 2)
            {
                if (words_and_emoticons_lower[i - 3].Equals("never") &&
                    (words_and_emoticons_lower[i - 2].Equals("so") || words_and_emoticons_lower[i - 2].Equals("this")) ||
                    (words_and_emoticons_lower[i - 1].Equals("so") || words_and_emoticons_lower[i - 1].Equals("this"))
                )
                {
                    adjustedValence *= 1.25;
                }
                else if (words_and_emoticons_lower[i - 3].Equals("without") &&
                    (words_and_emoticons_lower[i - 2].Equals("doubt") || words_and_emoticons_lower[i - 1].Equals("doubt"))
                )
                {

                }
                else if (IsNegated(new string[] { words_and_emoticons_lower[i - (start_i + 1)] }))
                {
                    adjustedValence *= Constants.N_SCALAR;
                }
            }

            return adjustedValence;
        }

        private double SpecialIdiomsCheck(double valence, string[] words_and_emoticons, int i)
        {
            var adjustedValence = valence;

            var words_and_emoticons_lower = words_and_emoticons.Select(s => s.ToLowerInvariant()).ToArray();

            var onezero = string.Format("{0} {1}", words_and_emoticons_lower[i - 1], words_and_emoticons_lower[i]);
            var twoonezero = string.Format("{0} {1} {2}", words_and_emoticons_lower[i - 2], words_and_emoticons_lower[i - 1], words_and_emoticons_lower[i - 0]);
            var twoone = string.Format("{0} {1}", words_and_emoticons_lower[i - 2], words_and_emoticons_lower[i - 1]);
            var threetwoone = string.Format("{0} {1} {2}", words_and_emoticons_lower[i - 3], words_and_emoticons_lower[i - 2], words_and_emoticons_lower[i - 1]);
            var threetwo = string.Format("{0} {1}", words_and_emoticons_lower[i - 3], words_and_emoticons_lower[i - 2]);

            var sequences = new string[] { onezero, twoonezero, twoone, threetwoone, threetwo };

            foreach (var seq in sequences)
            {
                if (Constants.SPECIAL_CASE_IDIOMS.ContainsKey(seq))
                {
                    adjustedValence = Constants.SPECIAL_CASE_IDIOMS[seq];

                    break;
                }
            }

            if (words_and_emoticons_lower.GetLength(0) - 1 > i)
            {
                var zeroone = string.Format("{0} {1}", words_and_emoticons_lower[i], words_and_emoticons_lower[i + 1]);

                if (Constants.SPECIAL_CASE_IDIOMS.ContainsKey(zeroone))
                {
                    adjustedValence = Constants.SPECIAL_CASE_IDIOMS[zeroone];
                }
            }

            if (words_and_emoticons_lower.GetLength(0) - 1 > i + 1)
            {
                var zeroonetwo = string.Format("{0} {1} {2}", words_and_emoticons_lower[i], words_and_emoticons_lower[i + 1], words_and_emoticons_lower[i + 2]);

                if (Constants.SPECIAL_CASE_IDIOMS.ContainsKey(zeroonetwo))
                {
                    adjustedValence = Constants.SPECIAL_CASE_IDIOMS[zeroonetwo];
                }
            }

            var n_grams = new string[] { threetwoone, threetwo, twoone };

            foreach (var n_gram in n_grams)
            {
                if (Constants.BOOSTER_DICT.ContainsKey(n_gram))
                {
                    adjustedValence += Constants.BOOSTER_DICT[n_gram];
                }
            }

            return adjustedValence;
        }

        // check for negation case using "least"
        private double LeastCheck(double valence, string[] words_and_emoticons, int i)
        {
            var adjustedValence = valence;

            if (i > 1 && !Lexicon.ContainsKey(words_and_emoticons[i - 1].ToLower()) && words_and_emoticons[i - 1].ToLower().Equals("least"))
            {
                if (!words_and_emoticons[i - 2].ToLower().Equals("at") && !words_and_emoticons[i - 2].ToLower().Equals("very"))
                {
                    adjustedValence *= Constants.N_SCALAR;
                }
            }
            else if (i > 0 && !Lexicon.ContainsKey(words_and_emoticons[i - 1].ToLower()) && words_and_emoticons[i - 1].ToLower().Equals("least"))
            {
                adjustedValence *= Constants.N_SCALAR;
            }

            return adjustedValence;
        }

        private bool IsNegated(string[] input_words, bool include_nt = true)
        {
            var input_words_lower = input_words.Select(s => s.ToLowerInvariant()).ToArray();

            foreach (var word in Constants.NEGATE)
            {
                if (input_words_lower.Contains(word))
                    return true;
            }

            if (include_nt)
            {
                foreach (var word in Constants.NEGATE)
                {
                    if (input_words_lower.Contains("n't"))
                    {
                        return true;
                    }
                }
            }

            if (input_words_lower.Contains("least"))
            {
                var i = Utility.Search(input_words, "least");

                if (i > 0 && !input_words[i - 1].Equals("at"))
                {
                    return true;
                }
            }

            return false;
        }

        private double ScalarIncDec(string word, double valence, bool IsCapDifferential)
        {
            var scalar = 0.0;
            var lowercase = word.ToLower();

            if (Constants.BOOSTER_DICT.ContainsKey(lowercase))
            {
                scalar = Constants.BOOSTER_DICT[lowercase];

                if (valence < 0)
                {
                    scalar *= -1;
                }

                // check if booster/dampener word is in ALLCAPS (while others aren't)
                if (word.All(char.IsUpper) && IsCapDifferential)
                {
                    if (valence > 0)
                    {
                        scalar += Constants.C_INCR;
                    }
                    else
                    {
                        scalar -= Constants.C_INCR;
                    }
                }
            }

            return scalar;
        }

        public double[] ButCheck(string[] words_and_emoticons, double[] sentiments)
        {
            // check for modification in sentiment due to contrastive conjunction 'but'
            var bi = Utility.Search(words_and_emoticons, "but");

            if (bi < 0)
            {
                bi = Utility.Search(words_and_emoticons, "BUT");
            }

            if (bi >= 0)
            {
                for (var si = 0; si < sentiments.GetLength(0); si++)
                {
                    if (si < bi)
                    {
                        sentiments[si] = sentiments[si] * 0.5;
                    }
                    else if (si > bi)
                    {
                        sentiments[si] = sentiments[si] * 1.5;
                    }
                }
            }

            return sentiments;
        }

        public double _punctuation_emphasis(string text)
        {
            // add emphasis from exclamation points and question marks
            var ep_amplifier = _amplify_ep(text);
            var qm_amplifier = _amplify_qm(text);
            var punct_emph_amplifier = ep_amplifier + qm_amplifier;

            return punct_emph_amplifier;
        }

        public double _amplify_ep(string text)
        {
            // check for added emphasis resulting from exclamation points (up to 4 of them)
            var ep_count = text.Count(c => c.Equals('!'));

            if (ep_count > 4)
            {
                ep_count = 4;
            }

            // (empirically derived mean sentiment intensity rating increase for
            // exclamation points)
            var ep_amplifier = ep_count * 0.292;

            return ep_amplifier;
        }

        public double _amplify_qm(string text)
        {
            // check for added emphasis resulting from question marks (2 or 3+)
            var qm_count = text.Count(c => c.Equals('?'));
            var qm_amplifier = 0.0;

            if (qm_count > 1)
            {
                if (qm_count <= 3)
                {
                    // (empirically derived mean sentiment intensity rating increase for
                    // question marks)
                    qm_amplifier = qm_count * 0.18;
                }
                else
                {
                    qm_amplifier = 0.96;
                }
            }
            return qm_amplifier;
        }

        public Tuple<double, double, int> _sift_sentiment_scores(double[] sentiments)
        {
            // want separate positive versus negative sentiment scores
            var pos_sum = 0.0;
            var neg_sum = 0.0;
            var neu_count = 0;

            foreach (var sentiment_score in sentiments)
            {
                if (sentiment_score > 0)
                {
                    pos_sum += sentiment_score + 1; // compensates for neutral words that are counted as 1
                }
                else if (sentiment_score < 0)
                {
                    neg_sum += sentiment_score - 1; // when used with math.fabs(), compensates for neutrals
                }
                else
                {
                    neu_count += 1;
                }
            }

            return new Tuple<double, double, int>(pos_sum, neg_sum, neu_count);
        }
    }
}
