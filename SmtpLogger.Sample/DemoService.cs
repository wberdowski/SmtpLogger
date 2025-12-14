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

                await Task.Delay(3300);
            }
        });

        var task2 = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    // throw test exception
                    var result = 0 / int.Parse("0");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Task failed!");
                }

                await Task.Delay(5700);
            }
        });
    }
}
