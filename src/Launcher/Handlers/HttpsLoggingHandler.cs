using NLog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class HttpLoggingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_logger.IsInfoEnabled)
        {
            var requestInfo = $"HTTP Request: {request.Method} {request.RequestUri}";
            if (request.Content != null)
            {
                requestInfo += $" | Content Headers: {request.Content.Headers}";
            }
            _logger.Info(requestInfo);
        }

        HttpResponseMessage? response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsErrorEnabled)
                _logger.Error(ex, "HTTP request failed");
            throw;
        }

        if (_logger.IsInfoEnabled && response != null)
        {
            var responseInfo = $"HTTP Response: {(int)response.StatusCode} {response.ReasonPhrase} for {request.RequestUri}";
            if (response.Content != null)
            {
                responseInfo += $" | Content Headers: {response.Content.Headers}";
            }
            _logger.Info(responseInfo);
        }

        return response!;
    }
}