using Azure.Communication.CallAutomation;

namespace CallAutomation.Scenarios.Utils
{
    public static class CollectTonesResultExtensions
    {
        public static string? CombineAll(this CollectTonesResult value)
        {
            // just numbers for now, can be added others well.
            var textToNumberMap = new Dictionary<string, string> {
                { "zero", "0" },
                { "one", "1" },
                { "two", "2" },
                { "three", "3" },
                { "four", "4" },
                { "five", "5" },
                { "six", "6" },
                { "seven", "7" },
                { "eight", "8" },
                { "nine", "9" } };

            List<string> result = new List<string>();
            foreach (var tone in value.Tones)
            {
                if (textToNumberMap.ContainsKey(tone.ToString()))
                {
                    result.Add(textToNumberMap[tone.ToString()]);
                }
            }

            return string.Concat(result);
        }
    }
}
