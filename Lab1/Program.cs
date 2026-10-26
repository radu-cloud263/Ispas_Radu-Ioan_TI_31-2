using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WordFrequency;

class Program
{
    static void Main()
    {
        string rootPath = @"D:\ReutersDataSet\Reuters";
        string stopWordsPath = Path.Combine(rootPath, "stopwords.txt");
        string outputPath = Path.Combine(rootPath, "vectori_frecvente.txt");

        var stopWords = new HashSet<string>(File.ReadAllLines(stopWordsPath).Select(w => w.ToLower()));
        var stemmer = new PorterStemmer();

        var globalDict = new Dictionary<string, int>();
        var documentResults = new List<string>();

        foreach (string file in Directory.GetFiles(rootPath, "*.xml", SearchOption.AllDirectories))
        {
            try
            {
                XDocument doc = XDocument.Load(file);

                string title = (string)doc.Descendants("title").FirstOrDefault();
                string text = (string)doc.Descendants("text").FirstOrDefault();

                string cleanText = Regex.Replace($"{title} {text}", @"[^\p{L}\s]+", " ").ToLower();

                var words = cleanText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => !stopWords.Contains(w))
                                     .Select(w => stemmer.StemWord(w));

                var localVector = new Dictionary<int, int>();
                foreach (string word in words)
                {
                    if (!globalDict.ContainsKey(word))
                        globalDict[word] = globalDict.Count;

                    int wordId = globalDict[word];
                    localVector[wordId] = localVector.GetValueOrDefault(wordId) + 1;
                }

                var topics = doc.Descendants("code")
                                .Where(c => (string)c.Parent.Attribute("class") == "bip:topics:1.0")
                                .Select(c => (string)c.Attribute("code"));

                string docName = Path.GetFileNameWithoutExtension(file);
                string folder = file.Contains("Training") ? "Training" : "Testing";
                string topicsStr = string.Join(" ", topics);

                string sparseStr = string.Join(" ", localVector.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}"));

                documentResults.Add($"{docName} # {sparseStr} # {topicsStr} # {folder}");
            }
            catch { }
        }

        using (StreamWriter sw = new StreamWriter(outputPath))
        {
            foreach (var word in globalDict.OrderBy(kv => kv.Value))
                sw.WriteLine($"@attribute {word.Key}");

            sw.WriteLine("@data");

            foreach (string docLine in documentResults)
                sw.WriteLine(docLine);
        }

        Console.WriteLine("Vectori generati cu succes");
    }
}