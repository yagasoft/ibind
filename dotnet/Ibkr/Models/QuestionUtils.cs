using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Ibkr;

namespace Ibkr.Models;

public static class QuestionUtils
{
    public static bool FindAnswer(string question, IDictionary<string, bool> answers)
    {
        foreach (var kv in answers)
        {
            if (question.Contains(kv.Key))
                return kv.Value;
        }
        throw new ArgumentException($"No answer found for question: \"{question}\"");
    }

    public static async Task<Result<JsonDocument>> HandleQuestionsAsync(
        Result<JsonDocument> originalResult,
        IDictionary<string, bool> answers,
        Func<string, bool, Task<Result<JsonDocument>>> replyCallback)
    {
        var result = new Result<JsonDocument>(originalResult.Data, originalResult.Request);
        var questions = new List<(string Question, bool Answer)>();

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var data = result.Data.RootElement;
            if (data.TryGetProperty("error", out var errorProp))
            {
                var errText = errorProp.GetRawText();
                if (errText.Contains("Order couldn't be submitted: Local order ID="))
                    throw new Exception($"Order couldn't be submitted. Orders are already registered.");
                throw new Exception($"While handling questions an error was returned: {errText}");
            }

            if (data.ValueKind != JsonValueKind.Array)
                throw new Exception($"While handling questions unknown data was returned: {data}");
            if (data.GetArrayLength() == 0)
                throw new Exception($"While handling questions unknown data was returned: {data}");

            var firstData = data[0];

            if (!firstData.TryGetProperty("message", out var messageProp))
            {
                JsonDocument finalDoc;
                if (data.GetArrayLength() == 1)
                    finalDoc = JsonDocument.Parse(firstData.GetRawText());
                else
                    finalDoc = JsonDocument.Parse(data.GetRawText());
                return new Result<JsonDocument>(finalDoc, originalResult.Request);
            }

            var question = messageProp[0].GetString()?.Trim().Replace("\n", "") ?? string.Empty;
            var answer = FindAnswer(question, answers);
            questions.Add((question, answer));

            if (answer)
            {
                var id = firstData.GetProperty("id").GetString()!;
                result = await replyCallback(id, true);
            }
            else
            {
                throw new Exception($"A question was not given a positive reply. Question: \"{question}\". Answers: {string.Join(", ", answers)}. Request: {originalResult.Request}");
            }
        }

        throw new Exception($"Too many questions: {originalResult.Data.RootElement.GetRawText()}: {JsonSerializer.Serialize(questions)}");
    }
}

