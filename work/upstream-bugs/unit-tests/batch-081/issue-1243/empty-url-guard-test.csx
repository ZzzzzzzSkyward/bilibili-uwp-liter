using System;
using System.Linq;
using System.Threading.Tasks;

sealed class TargetPrefix
{
    public async Task<bool> Handle(string url)
    {
            if (url.First() == '@')
            {
                return false;
            }
        return true;
    }
}

var threw = false;
string exceptionType = "";
try
{
    await new TargetPrefix().Handle("");
}
catch (Exception ex)
{
    threw = true;
    exceptionType = ex.GetType().Name;
}
Console.WriteLine($"Empty URL threw: {threw} ({exceptionType})");
var passed = !threw;
Console.WriteLine(passed
    ? "PASS: empty URL is handled without throwing"
    : "FAIL: empty URL guard throws before the unsupported-link fallback");
Environment.ExitCode = passed ? 0 : 1;
