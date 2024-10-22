// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

IEnumerable<int> GetRange(int count)
{
    List<int> results = new();
    int i;
    for (i = 1; i <= count; i++)
    {
        results.Add(i);
    }

    return results;
}

using CancellationTokenSource cancellationTokenSource = new();
HttpClientHandler httpClientHandler = new()
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
};
using HttpClient client = new(httpClientHandler);

var cancellationToken = cancellationTokenSource.Token;

var serverNumbers = GetRange(30);
var tryings = GetRange(100);

ParallelOptions parallelOptions = new()
{
    CancellationToken = cancellationToken,
    MaxDegreeOfParallelism = 10,
};

// foreach (var serverNumber in serverNumbers)
await Parallel.ForEachAsync(serverNumbers, parallelOptions, (serverNumber, innerCancellationToken1) =>
{
    List<Task> tasks = new();
    var url = $"Url with {serverNumber}";
    var name = $"<name value> ({serverNumber})";

    var requestUrl = $"{url}?name={Uri.EscapeDataString(name)}";

    // await Parallel.ForEachAsync(trying, parallelOptions, async (index, innerCancellationToken2) =>
    // {
    //     int? statusCode = null;
    //     Stopwatch stopwatch = new();
    //     try
    //     {
    //         stopwatch.Start();
    //         HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUrl);
    //         requestMessage.Headers.Add("cache-control", "no-cache");
    //         var responseMessage = await client.SendAsync(requestMessage, innerCancellationToken2);

    //         statusCode = (int)responseMessage.StatusCode;
    //     }
    //     catch
    //     {
    //         statusCode = null;
    //     }
    //     finally
    //     {
    //         stopwatch.Stop();
    //         Console.WriteLine("Request: {0}::{1}::HTTP{2}::{3}s", index, requestUrl, statusCode?.ToString() ?? "Unknown", stopwatch.Elapsed.ToString("ss.fff"));
    //     }
    // });

    foreach (var trying in tryings)
    {
        Stopwatch stopwatch = new();
        HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUrl);
        requestMessage.Headers.Add("cache-control", "no-cache");
        var requestTask = Task.Run(() =>
        {
            stopwatch.Start();
            return client.SendAsync(requestMessage, innerCancellationToken1);
        })
            .ContinueWith((res) =>
            {
                var statusCode = (int)res.Result.StatusCode;
                stopwatch.Stop();

                Console.WriteLine(@"Request: {0}::{1}::HTTP{2}::{3:ss\.fff}s", trying, requestUrl, statusCode.ToString() ?? "Unknown", stopwatch.Elapsed);
            }, TaskContinuationOptions.None);

        tasks.Add(requestTask);
    }

    Task.WaitAll(tasks.ToArray());

    return ValueTask.CompletedTask;
});