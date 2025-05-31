using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaimiraGames;
using static Logger.Logger;

namespace MarkovChains
{
    ///
    /// | word    | nextWord
    /// |         | i | want | to | eat | chinese | food |
    /// |---------|---|------|----|-----|---------|------|
    /// | i       | 0 | 1    | 0  | 0   | 0       | 0    |
    /// | want    | 0 | 0    | 1  | 0   | 0       | 0    |
    /// | to      | 0 | 0    | 0  | 1   | 0       | 0    |
    /// | eat     | 0 | 0    | 0  | 0   | 1       | 0    |
    /// | chinese | 0 | 0    | 0  | 0   | 0       | 1    |
    /// | food    | 0 | 0    | 0  | 0   | 0       | 0    |
    /// Ключи внешних и внутренних словарей всегда одинаковы.
    /// 
    /// Внешний словарь:
    /// i    = | i | want | to | eat | chinese | food |
    /// want = | i | want | to | eat | chinese | food |
    /// 
    /// Внутренний словарь:
    /// | i | want | to | eat | chinese | food |
    /// |---|------|----|-----|---------|------|
    /// | 0 | 1    | 0  | 0   | 0       | 0    |
    /// <summary>
    /// Класс, реализующий цепь Маркова.
    /// </summary>
    public class MarkovChain
    {
        [JsonInclude]
        private Dictionary<string, Dictionary<string, int>> _wordMatrix;
        // Два параметра для корректной печати
        [JsonInclude]
        private Dictionary<string, int> maxValueLength;
        [JsonInclude]
        private int maxKeyLength;
        public MarkovChain()
        {
            _wordMatrix = new Dictionary<string, Dictionary<string, int>>();
            // Длина самого длинного значения в столбце
            maxValueLength = new Dictionary<string, int>();
            maxKeyLength = 0;
        }
        public void AddText(string text)
        {
            AddText(text.Split());
        }
        public void AddText(string[] words)
        {
            if (words.Length < 2) return;
            for (int i = 1; i < words.Length; i++)
            {
                //Stopwatch stopwatch = Stopwatch.StartNew();
                _add(words[i - 1], words[i]);
                //stopwatch.Stop();
                //Log("{2, 10} тиков: {0} {1}", words[i - 1], words[i], stopwatch.ElapsedTicks);
            }
        }
        /// <summary>
        /// Добавляет во все словари новый ключ с нулевым значением.
        /// </summary>
        /// <param name="word"></param>
        private void _addKey(string word)
        {
            Dictionary<string, int> innerDictionaryIter, newWordDictionary;
            if (!_wordMatrix.ContainsKey(word))
            {
                // Новый внутренний словарь
                newWordDictionary = new Dictionary<string, int>();
                // За словом, которое мы ранее не видели, пока что ничего не следует.
                foreach (string key in _wordMatrix.Keys)
                {
                    newWordDictionary.Add(key, 0);
                }
                _wordMatrix.Add(word, newWordDictionary);
                // Ни в одном из внутренних словарей нет ключа newWord, добавляю
                // Иначе, синхронизирую ключи наружного словаря и ключи внутренних словарей
                foreach (string key in _wordMatrix.Keys)
                {
                    innerDictionaryIter = _wordMatrix[key];
                    innerDictionaryIter.Add(word, 0);
                }
                // Обновление значений, записанных для форматирования вывода на печать
                int l = word.Length;
                maxValueLength[word] = l;
                if (maxKeyLength < l) maxKeyLength = l;
            }
        }
        private void _add(string word, string nextWord)
        {
            if (!_wordMatrix.ContainsKey(word))
            {
                _wordMatrix.Add(word, new Dictionary<string, int>());

                int l = word.Length;
                if (!maxValueLength.ContainsKey(word) || maxValueLength[word] < l) maxValueLength[word] = l;
                if (maxKeyLength < l) maxKeyLength = l;
            }
            if (!_wordMatrix[word].ContainsKey(nextWord))
            {
                _wordMatrix[word].Add(nextWord, 0);
                int l = nextWord.Length;
                maxValueLength[nextWord] = l;
            }

            _wordMatrix[word][nextWord]++;

            int newValueLength = _wordMatrix[word][nextWord].ToString().Length;
            if (!maxValueLength.ContainsKey(nextWord) || maxValueLength[nextWord] < newValueLength) maxValueLength[nextWord] = newValueLength;
        }
        /// <summary>
        /// Возвращает взвешенный список, который используется один раз для определения следующего слова с учётом коэффициентов из текущей базы данных
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private WeightedList<string> _GetWeighedList(string word)
        {
            if (_wordMatrix.ContainsKey(word))
            {
                WeightedList<string> wl = new WeightedList<string>();
                wl.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;
                foreach (string key in _wordMatrix[word].Keys)
                {
                    int weight = _wordMatrix[word][key];
                    if (weight > 0) wl.Add(key, weight);
                }
                // Если у поданного слова нет вариантов последующих (оно последнее в тексте), то нужно выбрать случайное.
                if (wl.Count == 0) return _GetWeighedList();
                return wl;
            }
            else
            {
                // Если в качестве параметра передано неизвестное слово, то нужно выбрать случайное с равной вероятностью.
                return _GetWeighedList();
            }
        }
        private WeightedList<string> _GetWeighedList()
        {
            // Если в качестве параметра не подано слово, то нужно выбрать случайное с равной вероятностью.
            string[] keys = _wordMatrix.Keys.ToArray();
            WeightedListItem<string>[] wli = new WeightedListItem<string>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                wli[i] = new WeightedListItem<string>(keys[i], 1);
            }
            WeightedList<string> wl = new WeightedList<string>(wli);
            wl.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;
            return wl;
        }
        private string _next(string word = "")
        {
            if (_wordMatrix.Count < 1)
            {
                throw new ApplicationException("Невозможно выбрать следующее слово, так как нет исходных данных");
            }
            return _GetWeighedList(word).Next();
        }
        public string CreateText(int size = 50)
        {
            string result = string.Empty;
            string word;
            word = _next();
            result += word;
            for (int i = 0; i < size; i++)
            {
                word = _next(word);
                result += " " + word;
                //Log("Слово {0} из {1}.", i, size);
            }
            return result;
        }
        public override string ToString()
        {
            string result = "Текущее состояние экземпляра словесной матрицы:" + Environment.NewLine;
            // Отсортированные ключи, заголовки строк и столбцов
            //string[] keys = _wordMatrix.Keys.ToList().Order().ToArray();
            List<string> keys = _wordMatrix.Keys.ToList();
            foreach (string key in _wordMatrix.Keys)
            {
                foreach (string innerKey in _wordMatrix[key].Keys)
                {
                    if (!keys.Contains(innerKey))
                    {
                        keys.Add(innerKey);
                    }
                }
            }
            keys = keys.Order().ToList();

            string format;
            int l = keys.Count;
            int maxValueLength;
            result += "|" + new string(' ', maxKeyLength) + "|" + string.Join("|", keys) + "|" + Environment.NewLine;
            for (int i = 0; i < l; i++)
            {
                format = $"|{{0,{maxKeyLength}}}";
                result += string.Format(format, keys[i]);
                for (int j = 0; j < l; j++)
                {
                    maxValueLength = this.maxValueLength[keys[j]];
                    format = $"|{{0,{maxValueLength}}}";
                    Dictionary<string, int>? innnerDictionary;
                    int value = 0;
                    _wordMatrix.TryGetValue(keys[i], out innnerDictionary);
                    if (innnerDictionary is not null)
                    {
                        innnerDictionary.TryGetValue(keys[j], out value);
                    }
                    if (value == 0)
                    {
                        result += "|" + new string(' ', maxValueLength);
                    }
                    else
                    {
                        result += string.Format(format, _wordMatrix[keys[i]][keys[j]]);
                    }
                    Log("Строка {0}, столбец {1} из {2}", i, j, l);
                }
                result += string.Format("|" + Environment.NewLine);
            }
            return result;
        }
        internal void SerializeToJSON(string fileName)
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(fileName, jsonString);
            }
            catch
            {
                Log("Сохранение цепи Маркова в файл {0} не удалось.", fileName);
            }
        }
        internal static MarkovChain LoadFromJSON(string fileName)
        {
            try
            {
                string jsonString = File.ReadAllText(fileName);
                MarkovChain? a = JsonSerializer.Deserialize<MarkovChain>(jsonString);
                if (a is not null)
                {
                    Log("Цепь Маркова успешно загружена из файла {0}.", fileName);
                    return a;
                }
                return new MarkovChain();
            }
            catch
            {
                Log("Загрузка цепи Маркова из файла {0} не удалась.", fileName);
                return new MarkovChain();
            }
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            LogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                typeof(MarkovChain).Name
            );
            //string filePath = @"D:\Temp\Friyana.txt";
            string filePath = @"D:\Temp\output.txt";
            //string text;
            string[] lines;

            try
            {
                //text = File.ReadAllText(filePath);
                lines = File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                Log($"Error reading file: {ex.Message}");
                return;
            }

            MarkovChain wordMatrix = new MarkovChain();
            foreach (string line in lines)
            {
                string[] words = line.Split();
                wordMatrix.AddText(words);
            }
            Log(wordMatrix.ToString());
            wordMatrix.SerializeToJSON(@"D:\Temp\Markov.json");
            wordMatrix = MarkovChain.LoadFromJSON(@"D:\Temp\Markov.json");
            
            while (true)
            {
                Log(wordMatrix.CreateText());
                Console.ReadLine();
            }
        }
    }
}
