namespace WebApplication6.Models
{
    public class Phonetic
    {
        public string Text { get; set; }
        public string Audio { get; set; }
    }

    public class Definition
    {
        public string DefinitionText { get; set; }
        public string Example { get; set; }
        public List<string> Synonyms { get; set; }
        public List<string> Antonyms { get; set; }
    }

    public class Meaning
    {
        public string PartOfSpeech { get; set; }
        public List<Definition> Definitions { get; set; }
    }

    public class DictionaryEntry
    {
        public string Word { get; set; }
        public List<Phonetic> Phonetics { get; set; }
        public List<Meaning> Meanings { get; set; }
    }
}