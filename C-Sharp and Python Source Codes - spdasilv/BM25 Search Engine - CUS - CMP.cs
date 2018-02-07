using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.IO.Compression;
using System.Diagnostics;

// Stefanno Da Silva - 20508389
// 3A Management Engineering
// April 4th, 2016

// This program reads line by line a large compressed file
// containing several documents containing pieces of journals
// and reports. The program then generate an In-Memory
// Inverted Index of all the tokens contained within the
// document. This program ultilizes Custon (CUS) Tokenization
// to build the Inverted Index. CUS tokenization involves
// compoundig the tokens, checking for stop words and
// ultilizing the Porter Stemmer to find each token's
// corresponding root.
// The second step of the program is to read 45 different
// queries and seach the inverted index for related topics,
// producing a text file containing the top 1000 documents
// in terms of highest BM25 scores.

namespace BM25_Search_Engine___CUS___CMP
{
    public class PorterStemmer
    {

        // The passed in word turned into a char array. 
        // Quicker to use to rebuilding strings each time a change is made.
        private char[] wordArray;

        // Current index to the end of the word in the character array. This will
        // change as the end of the string gets modified.
        private int endIndex;

        // Index of the (potential) end of the stem word in the char array.
        private int stemIndex;


        /// <summary>
        /// Stem the passed in word.
        /// </summary>
        /// <param name="word">Word to evaluate</param>
        /// <returns></returns>
        public string StemWord(string word)
        {

            // Do nothing for empty strings or short words.
            if (string.IsNullOrWhiteSpace(word) || word.Length <= 2) return word;

            wordArray = word.ToCharArray();

            stemIndex = 0;
            endIndex = word.Length - 1;
            Step1();
            Step2();
            Step3();
            Step4();
            Step5();
            Step6();

            var length = endIndex + 1;
            return new String(wordArray, 0, length);
        }


        // Step1() gets rid of plurals and -ed or -ing.
        /* Examples:
			   caresses  ->  caress
			   ponies    ->  poni
			   ties      ->  ti
			   caress    ->  caress
			   cats      ->  cat

			   feed      ->  feed
			   agreed    ->  agree
			   disabled  ->  disable

			   matting   ->  mat
			   mating    ->  mate
			   meeting   ->  meet
			   milling   ->  mill
			   messing   ->  mess

			   meetings  ->  meet  		*/
        private void Step1()
        {
            // If the word ends with s take that off
            if (wordArray[endIndex] == 's')
            {
                if (EndsWith("sses"))
                {
                    endIndex -= 2;
                }
                else if (EndsWith("ies"))
                {
                    SetEnd("i");
                }
                else if (wordArray[endIndex - 1] != 's')
                {
                    endIndex--;
                }
            }
            if (EndsWith("eed"))
            {
                if (MeasureConsontantSequence() > 0)
                    endIndex--;
            }
            else if ((EndsWith("ed") || EndsWith("ing")) && VowelInStem())
            {
                endIndex = stemIndex;
                if (EndsWith("at"))
                    SetEnd("ate");
                else if (EndsWith("bl"))
                    SetEnd("ble");
                else if (EndsWith("iz"))
                    SetEnd("ize");
                else if (IsDoubleConsontant(endIndex))
                {
                    endIndex--;
                    int ch = wordArray[endIndex];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                        endIndex++;
                }
                else if (MeasureConsontantSequence() == 1 && IsCVC(endIndex)) SetEnd("e");
            }
        }

        // Step2() turns terminal y to i when there is another vowel in the stem.
        private void Step2()
        {
            if (EndsWith("y") && VowelInStem())
                wordArray[endIndex] = 'i';
        }

        // Step3() maps double suffices to single ones. so -ization ( = -ize plus
        // -ation) maps to -ize etc. note that the string before the suffix must give m() > 0. 
        private void Step3()
        {
            if (endIndex == 0) return;

            /* For Bug 1 */
            switch (wordArray[endIndex - 1])
            {
                case 'a':
                    if (EndsWith("ational")) { ReplaceEnd("ate"); break; }
                    if (EndsWith("tional")) { ReplaceEnd("tion"); }
                    break;
                case 'c':
                    if (EndsWith("enci")) { ReplaceEnd("ence"); break; }
                    if (EndsWith("anci")) { ReplaceEnd("ance"); }
                    break;
                case 'e':
                    if (EndsWith("izer")) { ReplaceEnd("ize"); }
                    break;
                case 'l':
                    if (EndsWith("bli")) { ReplaceEnd("ble"); break; }
                    if (EndsWith("alli")) { ReplaceEnd("al"); break; }
                    if (EndsWith("entli")) { ReplaceEnd("ent"); break; }
                    if (EndsWith("eli")) { ReplaceEnd("e"); break; }
                    if (EndsWith("ousli")) { ReplaceEnd("ous"); }
                    break;
                case 'o':
                    if (EndsWith("ization")) { ReplaceEnd("ize"); break; }
                    if (EndsWith("ation")) { ReplaceEnd("ate"); break; }
                    if (EndsWith("ator")) { ReplaceEnd("ate"); }
                    break;
                case 's':
                    if (EndsWith("alism")) { ReplaceEnd("al"); break; }
                    if (EndsWith("iveness")) { ReplaceEnd("ive"); break; }
                    if (EndsWith("fulness")) { ReplaceEnd("ful"); break; }
                    if (EndsWith("ousness")) { ReplaceEnd("ous"); }
                    break;
                case 't':
                    if (EndsWith("aliti")) { ReplaceEnd("al"); break; }
                    if (EndsWith("iviti")) { ReplaceEnd("ive"); break; }
                    if (EndsWith("biliti")) { ReplaceEnd("ble"); }
                    break;
                case 'g':
                    if (EndsWith("logi"))
                    {
                        ReplaceEnd("log");
                    }
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void Step4()
        {
            switch (wordArray[endIndex])
            {
                case 'e':
                    if (EndsWith("icate")) { ReplaceEnd("ic"); break; }
                    if (EndsWith("ative")) { ReplaceEnd(""); break; }
                    if (EndsWith("alize")) { ReplaceEnd("al"); }
                    break;
                case 'i':
                    if (EndsWith("iciti")) { ReplaceEnd("ic"); }
                    break;
                case 'l':
                    if (EndsWith("ical")) { ReplaceEnd("ic"); break; }
                    if (EndsWith("ful")) { ReplaceEnd(""); }
                    break;
                case 's':
                    if (EndsWith("ness")) { ReplaceEnd(""); }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void Step5()
        {
            if (endIndex == 0) return;

            switch (wordArray[endIndex - 1])
            {
                case 'a':
                    if (EndsWith("al")) break; return;
                case 'c':
                    if (EndsWith("ance")) break;
                    if (EndsWith("ence")) break; return;
                case 'e':
                    if (EndsWith("er")) break; return;
                case 'i':
                    if (EndsWith("ic")) break; return;
                case 'l':
                    if (EndsWith("able")) break;
                    if (EndsWith("ible")) break; return;
                case 'n':
                    if (EndsWith("ant")) break;
                    if (EndsWith("ement")) break;
                    if (EndsWith("ment")) break;
                    /* element etc. not stripped before the m */
                    if (EndsWith("ent")) break; return;
                case 'o':
                    if (EndsWith("ion") && stemIndex >= 0 && (wordArray[stemIndex] == 's' || wordArray[stemIndex] == 't')) break;
                    /* j >= 0 fixes Bug 2 */
                    if (EndsWith("ou")) break; return;
                /* takes care of -ous */
                case 's':
                    if (EndsWith("ism")) break; return;
                case 't':
                    if (EndsWith("ate")) break;
                    if (EndsWith("iti")) break; return;
                case 'u':
                    if (EndsWith("ous")) break; return;
                case 'v':
                    if (EndsWith("ive")) break; return;
                case 'z':
                    if (EndsWith("ize")) break; return;
                default:
                    return;
            }
            if (MeasureConsontantSequence() > 1)
                endIndex = stemIndex;
        }

        /* step6() removes a final -e if m() > 1. */
        private void Step6()
        {
            stemIndex = endIndex;

            if (wordArray[endIndex] == 'e')
            {
                var a = MeasureConsontantSequence();
                if (a > 1 || a == 1 && !IsCVC(endIndex - 1))
                    endIndex--;
            }
            if (wordArray[endIndex] == 'l' && IsDoubleConsontant(endIndex) && MeasureConsontantSequence() > 1)
                endIndex--;
        }

        // Returns true if the character at the specified index is a consonant.
        // With special handling for 'y'.
        private bool IsConsonant(int index)
        {
            var c = wordArray[index];
            if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u') return false;
            return c != 'y' || (index == 0 || !IsConsonant(index - 1));
        }

        /* m() measures the number of consonant sequences between 0 and j. if c is
		   a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
		   presence,

			  <c><v>       gives 0
			  <c>vc<v>     gives 1
			  <c>vcvc<v>   gives 2
			  <c>vcvcvc<v> gives 3
			  ....		*/
        private int MeasureConsontantSequence()
        {
            var n = 0;
            var index = 0;
            while (true)
            {
                if (index > stemIndex) return n;
                if (!IsConsonant(index)) break; index++;
            }
            index++;
            while (true)
            {
                while (true)
                {
                    if (index > stemIndex) return n;
                    if (IsConsonant(index)) break;
                    index++;
                }
                index++;
                n++;
                while (true)
                {
                    if (index > stemIndex) return n;
                    if (!IsConsonant(index)) break;
                    index++;
                }
                index++;
            }
        }

        // Return true if there is a vowel in the current stem (0 ... stemIndex)
        private bool VowelInStem()
        {
            int i;
            for (i = 0; i <= stemIndex; i++)
            {
                if (!IsConsonant(i)) return true;
            }
            return false;
        }

        // Returns true if the char at the specified index and the one preceeding it are the same consonants.
        private bool IsDoubleConsontant(int index)
        {
            if (index < 1) return false;
            return wordArray[index] == wordArray[index - 1] && IsConsonant(index);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
		   and also if the second c is not w,x or y. this is used when trying to
		   restore an e at the end of a short word. e.g.

			  cav(e), lov(e), hop(e), crim(e), but
			  snow, box, tray.		*/
        private bool IsCVC(int index)
        {
            if (index < 2 || !IsConsonant(index) || IsConsonant(index - 1) || !IsConsonant(index - 2)) return false;
            var c = wordArray[index];
            return c != 'w' && c != 'x' && c != 'y';
        }

        // Does the current word array end with the specified string.
        private bool EndsWith(string s)
        {
            var length = s.Length;
            var index = endIndex - length + 1;
            if (index < 0) return false;

            for (var i = 0; i < length; i++)
            {
                if (wordArray[index + i] != s[i]) return false;
            }
            stemIndex = endIndex - length;
            return true;
        }

        // Set the end of the word to s.
        // Starting at the current stem pointer and readjusting the end pointer.
        private void SetEnd(string s)
        {
            var length = s.Length;
            var index = stemIndex + 1;
            for (var i = 0; i < length; i++)
            {
                wordArray[index + i] = s[i];
            }
            // Set the end pointer to the new end of the word.
            endIndex = stemIndex + length;
        }

        // Conditionally replace the end of the word
        private void ReplaceEnd(string s)
        {
            if (MeasureConsontantSequence() > 0) SetEnd(s);
        }
    }

    //The class DocScore store a document
    // identification and BM25 score.
    class DocScore
    {
        public string doc_id;
        public double doc_score;

        public DocScore(string doc_id, double doc_score)
        {
            this.doc_id = doc_id;
            this.doc_score = doc_score;
        }
    }

    // The class PriorityQueue stores a queue of the class
    // DocScore. The queue is organizaed in a manner that
    // The DocScore with the higher BM25 score is always at
    // the top of the queue. The queue is organized in the
    // form of a heap data structure.
    class PriorityQueue
    {
        public static List<DocScore> queue;

        public PriorityQueue()
        {
            queue = new List<DocScore>();
        }

        // This method adds a new DocScore into the heap.
        // The new DocSocre is adde at the end of the list
        // and shifted up if its BM25 score is higher than
        // its parent.
        public void add_shift(string doc_id, double doc_score)
        {
            DocScore new_Doc = new DocScore(doc_id, doc_score);
            queue.Add(new_Doc);
            if (queue.Count > 0)
            {
                int k = queue.Count - 1;
                while (k > 0)
                {
                    int p = (k - 1) / 2;
                    double k_value = queue[k].doc_score;
                    double p_value = queue[p].doc_score;
                    if (k_value > p_value)
                    {
                        // Move up
                        DocScore tmp_k =
                            new DocScore(queue[k].doc_id, queue[k].doc_score);
                        queue[k] = queue[p];
                        queue[p] = tmp_k;
                        k = p;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                //Do nothnig.
            }
        }

        // This method writes to a textfile the document ID
        // and the BM25 score of the item standing at the 
        // top of the heap.
        // The last document of the heap is then put into
        // the highest position and shifted down if its 
        // BM25 value us smaller than any of its children.
        public void Output_Ranks(string topic_id, StreamWriter sw)
        {
            int count = 0;
            while (queue.Count > 0 &&  count < 1000)
            {
                sw.WriteLine(topic_id
                    + " q0 " + queue[0].doc_id
                    + " " + (count + 1)
                    + " " + queue[0].doc_score
                    + " spdasilv");
                queue[0] = queue[queue.Count - 1];
                queue.RemoveAt(queue.Count - 1);
                ++count;
                int k = 0;
                int l = 2 * k + 1;
                while (l < queue.Count)
                {
                    int max = l;
                    int r = l + 1;
                    if (r < queue.Count)
                    {
                        if (queue[r].doc_score > queue[l].doc_score)
                        {
                            ++max;
                        }
                    }
                    if (queue[k].doc_score < queue[max].doc_score)
                    {
                        DocScore tmp_k =
                            new DocScore(queue[k].doc_id, queue[k].doc_score);
                        queue[k] = queue[max];
                        queue[max] = tmp_k;
                        k = max;
                        l = 2 * k + 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // This switch serves a method to control when text
            // whould be tokenized or not. For any time the
            // variable is bigger than 0, the program will
            // tokenize the string.
            int switch_On = 0;
            int count = 0; // Counts number of documents.
            bool locking = true; // If false, it means we are
                                 //  inside a document.

            //Open and read document.
            string filename = "latimes.gz";
            FileStream instream =
                new FileStream(filename, FileMode.Open);
            GZipStream gzStream =
                new GZipStream(instream, CompressionMode.Decompress);
            StreamReader sr = new StreamReader(gzStream);

            // Token_List temporarily stores the tokens 
            // and their counts for a single document.
            // Dictionary_List store Token_List dictionaries.
            // The inverted_Index stores each term as a key and
            // tuples of document ID and term count.
            // Doc_Lenght stores each document respective lennght.
            Dictionary<string, int> Token_List = new Dictionary<string, int>();
            Dictionary<string, SortedList<string, int>> inverted_Index =
                new Dictionary<string, SortedList<string, int>>();
            Dictionary<string, int> Doc_Lenght = new Dictionary<string, int>();
            string line = sr.ReadLine();
            string curr_Doc = null;
            List<string> stop_words = new List<string>();
            stop_words = Get_StopWords("Stop Words.txt");
            while (line != null)
            {
                // Evaluates if a new document was found. It then takes
                // the DocID and creates a Token_List Dictionary.
                if (line == "<DOC>")
                {
                    ++count;
                    line = sr.ReadLine();
                    string[] Doc_ID = line.Split();
                    curr_Doc = Doc_ID[1];
                    Token_List = new Dictionary<string, int>();
                    line = sr.ReadLine();
                    locking = false;
                    continue;
                }

                // When the loop reaches the end of the document it takes
                // the current Token_List instance and transforms it into
                // a list for easier iteration. The program evaluates each
                // term in the list and either adds a new term to the 
                // inverted index or, if already present, appends a new
                // document ID and term count tuple to a specific term.
                else if (line == "</DOC>" && locking == false)
                {
                    List<KeyValuePair<string, int>> myList = Token_List.ToList();
                    myList.Sort((firstPair, nextPair) =>
                    {
                        return firstPair.Value.CompareTo(nextPair.Value);
                    }
                    );
                    Doc_Lenght.Add(curr_Doc, Get_Length(myList));
                    for (int i = 0; i < Token_List.Count; ++i)
                    {
                        if (inverted_Index.ContainsKey(myList[i].Key))
                        {
                            inverted_Index[myList[i].Key].Add
                                (curr_Doc, myList[i].Value);
                        }
                        else
                        {
                            SortedList<string, int> tmp_list =
                                new SortedList<string, int>();
                            tmp_list.Add(curr_Doc,
                                myList[i].Value);
                            inverted_Index.Add(myList[i].Key, tmp_list);
                        }
                    }
                    locking = true;
                    continue;
                }

                // The code below check if we should tokenize the strings
                // inside certain parts of the document or not.
                // As long the string are inside at least one of the
                // possible tags, it will be tokenized.
                else if (line == "<HEADLINE>" ||
                    line == "<TEXT>" ||
                    line == "<GRAPHIC>" ||
                    line == "<SUBJECT>")
                {
                    ++switch_On;
                    line = sr.ReadLine();
                    continue;
                }
                else if (line == "</HEADLINE>" ||
                    line == "</TEXT>" ||
                    line == "</GRAPHIC>" ||
                    line == "</SUBJECT>")
                {
                    --switch_On;
                    line = sr.ReadLine();
                    continue;
                }
                if (switch_On > 0 && locking == false && !line[0].Equals('<'))
                {
                    Token_List = Tokenize(line, Token_List, stop_words);
                }
                line = sr.ReadLine();
            }
            sr.Close();

            double avg_length = (Doc_Lenght.Sum(x => x.Value) / count);
            StreamReader query_read = new StreamReader("Queries.txt");
            StreamWriter sw = new StreamWriter("Rankings - spdasilv.txt");
            string curr_query = query_read.ReadLine();

            // Create new stopwatch and start it.
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // This loop read each query and generates a ranking for the
            // top 1000 documents that may be relevant to the topic.
            // It first obtains the topic number and query. The program
            // then tokenizes the query and for each query obtains the
            // BM25 score of all the documents that contain any of the 
            // words in the query. They are added to a priority queue and
            // then the queue outputs the 1000 highest scores.
            while (curr_query != null)
            {
                string[] curr_query_arr = curr_query.Split(':');
                string[] curr_topic_arr = curr_query_arr[0].Split(' ');
                Dictionary<string, int> new_query =
                    new Dictionary<string, int>();
                new_query = Tokenize(curr_query_arr[1],
                    new_query, stop_words);
                Dictionary<string, double> relevant_docs =
                    search_query(new_query,
                    inverted_Index,
                    Doc_Lenght,
                    avg_length);
                PriorityQueue rankings = Organize_Queue(relevant_docs);
                rankings.Output_Ranks(curr_topic_arr[1], sw);
                curr_query = query_read.ReadLine();
            }
            sw.Flush();
            sw.Close();
            query_read.Close();

            // Stop stopwatch and output value.
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
            Console.WriteLine("SEARCH COMPLETE. Press any key to exit...");
            Console.ReadKey();
        }

        // This methods reads a texfile containing a list of
        // stop words that should be ignored when building
        // the inverted index and reading queries. The
        // method return a List of type string.
        static public List<string> Get_StopWords(string location)
        {
            StreamReader sr = new StreamReader(location);
            List<string> list = new List<string>();
            string line = sr.ReadLine();
            while (line != null)
            {
                list.Add(line);
                line = sr.ReadLine();
            }

            return list;
        }
        static public int Get_Length(List<KeyValuePair<string, int>> token_list)
        {
            int length = 0;
            for (int i = 0; i < token_list.Count; ++i)
            {
                length = length + token_list[i].Value;
            }
            return length;
        }

        // This method takes a string and strips all
        // special character of the string. If a number
        // is found close to a letter, both characters
        // are then split.
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            char prev = ' ';
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'z'))
                {
                    if (((c >= 'a' && c <= 'z') &&
                        (prev >= '0' && prev <= '9')) ||
                        ((prev >= 'a' && prev <= 'z') &&
                        (c >= '0' && c <= '9')))
                    {
                        sb.Append(' ');
                    }
                    sb.Append(c);
                    prev = c;
                }
                else if (prev != ' ')
                {
                    sb.Append(' ');
                    prev = ' ';
                }
            }
            return sb.ToString();
        }

       // This is the Custom Tokenization model.
       // Initially the text to be tokenized is transformed into a sequence
       // of lowercase characters and split on whitespace.Each word is then
       // stripped of any special character while numbers and letters are
       // separated.As an example “international-no02” is split into the
       // tokens “international”, “no”, “02”. For each token we verify if
       // they are contained within a list of 418 stop words, and, if they
       // are the token is thrown away and will not be added to the inverted
       // index nor the compounded token.Each token is then stemmed and then
       // united once more to generate the compounded token.At the end of the
       // process “International-No02” is divided into 4 tokens: “intern”, 
       // “no”, “02” and “internno02”.
        static public Dictionary<string, int>
           Tokenize(string text, Dictionary<string, int> tmp_Token,
            List<string> stop_words)
        {
            List<string> tokens = new List<string>();
            text = text.ToLower();
            var stemmer = new PorterStemmer();
            string stem = "";
            string[] text_arr = text.Split(' ');
            string word = "";
            string[] word_arr;
            for (int j = 0; j < text_arr.Length; ++j)
            {
                int compund_count = 0;
                string compound = "";
                word = RemoveSpecialCharacters(text_arr[j]);
                word = word.Trim();
                word_arr = word.Split(new char[0]);
                for (int g = 0; g < word_arr.Length; ++g)
                {
                    if (!stop_words.Contains(word_arr[g]) 
                        && word_arr[g] != "")
                    {
                        stem = stemmer.StemWord(word_arr[g]);
                        tokens.Add(stem);
                        compound += stem;
                        ++compund_count;
                    }
                }
                if (compund_count > 1)
                {
                    tokens.Add(compound);
                }

            }
            for (int j = 0; j < tokens.Count; ++j)
            {
                if (!tmp_Token.ContainsKey(tokens[j]))
                {
                    tmp_Token.Add(tokens[j], 1);
                }
                else
                {
                    tmp_Token[tokens[j]] += 1;
                }
            }

            return tmp_Token;
        }

        // The method below iterates through each term
        // present within the query and finds its partial
        // BM25 score. The partial scores for all terms are
        // added to each other forming the total score. The
        // method returns a dictionary containing all the
        // documents and their scores.
        static public Dictionary<string, double>
            search_query(Dictionary<string, int> query,
            Dictionary<string, SortedList<string, int>> inverted_Index,
            Dictionary<string, int> Doc_Length,
            double avg_length)
        {
            List<KeyValuePair<string,
                SortedList<string, int>>> selected_indexes =
                new List<KeyValuePair<string, SortedList<string, int>>>();
            Dictionary<string, double> relevant_docs =
                new Dictionary<string, double>();
            foreach (KeyValuePair<string, int> entry in query)
            {
                if (inverted_Index.ContainsKey(entry.Key))
                {
                    selected_indexes.Add(new KeyValuePair<string,
                        SortedList<string, int>>(entry.Key,
                        inverted_Index[entry.Key]));

                }
            }

            // Parameters: k1=1.2, b=0.75, k2=7, both R and r assumed to be 0.
            foreach (KeyValuePair<string, SortedList<string, int>>
                entry in selected_indexes)
            {
                foreach (KeyValuePair<string, int> doc in entry.Value)
                {
                    if (!relevant_docs.ContainsKey(doc.Key))
                    {
                        double K = 1.2 * ((1 - 0.75) + 0.75 *
                            (Doc_Length[doc.Key] / avg_length));
                        double score =
                            Math.Log
                            (((0.5) / (0.5)) / ((entry.Value.Count + 0.5) /
                            (Doc_Length.Count - entry.Value.Count + 0.5)))
                            * ((2.2 * doc.Value) / (K + doc.Value))
                            * ((8 * query[entry.Key]) / (7 + query[entry.Key]));
                        relevant_docs.Add(doc.Key, score);
                    }
                    else
                    {
                        double K = 1.2 * ((1 - 0.75) + 0.75 *
                            (Doc_Length[doc.Key] / avg_length));
                        double score =
                            Math.Log
                            (((0.5) / (0.5)) / ((entry.Value.Count + 0.5) /
                            (Doc_Length.Count - entry.Value.Count + 0.5)))
                            * ((2.2 * doc.Value) / (K + doc.Value))
                            * ((8 * query[entry.Key]) / (7 + query[entry.Key]));
                        relevant_docs[doc.Key] = relevant_docs[doc.Key] + score;
                    }
                }
            }

            return relevant_docs;
        }

        // The method here takes the dictionary produced in
        // the other method "search_query" and adds each
        // entry to a priority queue.
        static public PriorityQueue Organize_Queue
            (Dictionary<string, double> relevant_docs)
        {
            PriorityQueue rankings = new PriorityQueue();
            foreach (KeyValuePair<string, double> entry in relevant_docs)
            {
                rankings.add_shift(entry.Key, entry.Value);
            }

            return rankings;
        }
    }
}