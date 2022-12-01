using System.Text.Json.Serialization;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

public class SentimentAnalysis
{
    private static readonly Uri CognitiveServicesUri = new ("<COGNITIVE_SERVICE_URI>");
    private static readonly AzureKeyCredential CognitiveServicesKey = new ("<COGNITIVE_SERVICE_KEY>");
    private static readonly string SpeechKey = "<SPEECH_KEY>";
    private static readonly string SpeechRegion = "<SPEECH_REGION>";

    public static async Task<Dictionary<string, SentimentResult>> AnalyzeSentiment(string filePath)
    {
        var recognizedText = await SpeechToTextFromFile(filePath);
        var textAnalysisClient = new TextAnalyticsClient(CognitiveServicesUri, CognitiveServicesKey);

        var response = await textAnalysisClient.AnalyzeSentimentAsync(recognizedText);
        var sentiments = GetSentiments(response.Value);

        return sentiments;
    }

    /**
     * <summary>
     * Speech to text using file input.
     * </summary>
     */
    private static async Task<string> SpeechToTextFromFile(string filePath)
    {
        var speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);

        // Creates an intent recognizer using file as audio input.
        using var audioInput = AudioConfig.FromWavFileInput(filePath);
        using var recognizer = new SpeechRecognizer(speechConfig, audioInput);

        // The TaskCompletionSource to stop recognition.
        var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recognizedSpeech = "";

        // Subscribes to events.
        recognizer.Recognizing += (s, e) =>
        {
            Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
        };

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                recognizedSpeech += $" {e.Result.Text}";
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine("NOMATCH: Speech could not be recognized.");
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            Console.WriteLine($"CANCELED: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                Console.WriteLine("CANCELED: Did you update the subscription info?");
            }

            stopRecognition.TrySetResult(0);
        };

        recognizer.SessionStarted += (s, e) =>
        {
            Console.WriteLine("\n    Session started event.");
        };

        recognizer.SessionStopped += (s, e) =>
        {
            Console.WriteLine("\n    Session stopped event.");
            Console.WriteLine("\nStop recognition.");
            stopRecognition.TrySetResult(0);
        };

        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

        // Waits for completion.
        Task.WaitAny(stopRecognition.Task);

        // Stops recognition.
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        return recognizedSpeech;
    }

    /**
     * <summary>
     * Get sentiment from text.
     * </summary>
     */
    private static Dictionary<string, SentimentResult> GetSentiments(DocumentSentiment documentSentiment)
    {
        var sentiments = new Dictionary<string, SentimentResult>();

        Console.WriteLine($"Results of \"Sentiment Analysis\" Model\n");
        Console.WriteLine($"Document sentiment is {documentSentiment.Sentiment}, with confidence scores: ");
        Console.WriteLine($"  Positive confidence score: {documentSentiment.ConfidenceScores.Positive}.");
        Console.WriteLine($"  Neutral confidence score: {documentSentiment.ConfidenceScores.Neutral}.");
        Console.WriteLine($"  Negative confidence score: {documentSentiment.ConfidenceScores.Negative}.");
        Console.WriteLine("");
        Console.WriteLine($"  Sentence sentiment results:");

        sentiments.Add("Overall Sentiment", new SentimentResult
        {
            Sentiment = documentSentiment.Sentiment,
            ConfidenceScores = documentSentiment.ConfidenceScores
        });

        foreach (SentenceSentiment sentimentInSentence in documentSentiment.Sentences)
        {
            Console.WriteLine($"  For sentence: \"{sentimentInSentence.Text}\"");
            Console.WriteLine($"  Sentiment is {sentimentInSentence.Sentiment}, with confidence scores: ");
            Console.WriteLine($"    Positive confidence score: {sentimentInSentence.ConfidenceScores.Positive}.");
            Console.WriteLine($"    Neutral confidence score: {sentimentInSentence.ConfidenceScores.Neutral}.");
            Console.WriteLine($"    Negative confidence score: {sentimentInSentence.ConfidenceScores.Negative}.");
            Console.WriteLine("");
            sentiments.Add(sentimentInSentence.Text, new SentimentResult
            {
                Sentiment = sentimentInSentence.Sentiment,
                ConfidenceScores = sentimentInSentence.ConfidenceScores
            });
        }

        return sentiments;
    }

    public class SentimentResult
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TextSentiment Sentiment { get; set; }
    
        public SentimentConfidenceScores ConfidenceScores { get; set; }
    }
}