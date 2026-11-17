using System.Text.Json;
using BirthdayReminder.Models;

namespace BirthdayReminder.Services;

public sealed class GreetingCardService(HttpClient httpClient, ILogger<GreetingCardService> logger)
{
    private static readonly GreetingCard Fallback = new(
        "Wishing you a wonderful birthday filled with joy, laughter, and memorable moments!",
        "Birthday Reminder",
        "local fallback",
        DateTimeOffset.UtcNow);

    public async Task<GreetingCard> GetFreshGreetingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("random?tags=happy,happiness,life", cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var quote = await JsonSerializer.DeserializeAsync<QuoteResponse>(stream, cancellationToken: cancellationToken);
            if (quote is null || string.IsNullOrWhiteSpace(quote.Content))
            {
                throw new JsonException("The greeting service returned an empty response.");
            }

            return new GreetingCard(quote.Content, quote.Author ?? "Unknown", "quotable.io", DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Online greeting service was unavailable; using the local greeting.");
            return Fallback with { RetrievedAt = DateTimeOffset.UtcNow };
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Online greeting service timed out; using the local greeting.");
            return Fallback with { RetrievedAt = DateTimeOffset.UtcNow };
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Online greeting service returned invalid data; using the local greeting.");
            return Fallback with { RetrievedAt = DateTimeOffset.UtcNow };
        }
    }

    private sealed record QuoteResponse(string? Content, string? Author);
}
