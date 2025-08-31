using NLog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly Logger _logger;

    public HttpLoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _logger = LogManager.GetCurrentClassLogger();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info(request.ToString());
            var response = await base.SendAsync(request, cancellationToken);
            _logger.Info(response.ToString());
            return response;
        }

        catch (Exception ex)
        {
            _logger.Error(ex, "HTTP request failed");
            throw;
        }
    }
}
