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
using System.Linq;
using System.Text.RegularExpressions;

namespace VaderSentimentAnalysis
{
    // Identify sentiment-relevant string-level properties of input text
    public class SentimentText
    {
        private readonly string _text;
        public string[] WordsAndEmoticons;
        public bool IsAllCapsDifferential;

        public SentimentText(string text)
        {
            _text = text;
            WordsAndEmoticons = WordsAndEmoticonsOnly();
            IsAllCapsDifferential = AllCapsDifferential(WordsAndEmoticons);
        }

        readonly string[] PunctuationList = { ".", "!", "?", ",", ";", ":", "-", "'", "\"", "!!", "!!!", "??", "???", "?!?", "!?!", "?!?!", "!?!?" };

        public string StripPunctuations()
        {
            return Regex.Replace(_text, @"\p{P}+", "");
        }

        public int Count(string[] words, string word)
        {
            var counts = new Dictionary<string, int>();

            foreach (string key in words)
            {
                if (counts.ContainsKey(key))
                {
                    counts[key]++;
                }
                else
                {
                    counts.Add(key, 1);
                }
            }

            return counts.ContainsKey(word) ? counts[word] : 0;
        }

        // Check whether just some words in the input are ALL CAPS 
        public bool AllCapsDifferential(string[] words)
        {
            var AllCapsWords = 0;

            foreach (var word in words)
            {
                if (word.All(char.IsUpper))
                {
                    AllCapsWords++;
                }
            }

            var cap_differential = words.GetLength(0) - AllCapsWords;

            return cap_differential > 0 && cap_differential < words.GetLength(0);
        }

        string[] WordsOnly()
        {
            // removes punctuation (but loses emoticons & contractions)
            var modifiedText = StripPunctuations();

            // split on white space
            var words = Regex.Split(modifiedText, @"\s+");

            // get rid of empty items or single letter "words" like 'a' and 'I'
            var fullWords = words.Where(key => key.Length > 1).ToArray();

            return fullWords;
        }

        string[] WordsAndEmoticonsOnly()
        {
            // split on white space
            var wordsAndEmoticons = Regex.Split(_text, @"\s+");

            // get rid of empty items or single letter "words" like 'a' and 'I'
            var filtered = wordsAndEmoticons.Where(key => key.Length > 1).ToArray();

            var wordsOnly = WordsOnly();

            foreach (var word in wordsOnly)
            {
                foreach (var punctuation in PunctuationList)
                {
                    // replace all punctuation + word combinations with word
                    var punctuationWord = string.Concat(punctuation, word);

                    var x1 = Count(filtered, punctuationWord);

                    while (x1 > 0)
                    {
                        var index = Utility.Search(filtered, punctuationWord);

                        if (index >= 0)
                        {
                            filtered[index] = word;
                        }

                        x1 = Count(filtered, punctuationWord);
                    }

                    // do the same as above but word then punctuation
                    var wordPunctuation = string.Concat(word, punctuation);

                    var x2 = Count(filtered, wordPunctuation);

                    while (x2 > 0)
                    {
                        var index = Utility.Search(filtered, wordPunctuation);

                        if (index >= 0)
                        {
                            filtered[index] = word;
                        }

                        x2 = Count(filtered, wordPunctuation);
                    }
                }
            }

            return filtered;
        }
    }
}
