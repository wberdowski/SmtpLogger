using Microsoft.Extensions.Logging;

namespace SmtpLogger.Sample;
internal sealed class DemoService
{
    private readonly ILogger<DemoService> _logger;

    public DemoService(ILogger<DemoService> logger)
    {
        _logger = logger;
    }

    public void DoSomethingAndThrow()
    {
        var task = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    // throw test exception
                    uint.Parse("-1");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Task failed!");
                }

                await Task.Delay(5000);
            }
        });
    }
}
