using System;
using System.Linq;
using System.Threading.Tasks;

sealed class TargetPrefix
{
    public async Task<bool> Handle(string url)
    {

            if (url.Length == 0)
            {
                return false;
            }
            if (url.First() == '@')
            {
                return false;
            }
                    return true;
    }
}

var handler = new TargetPrefix();
var emptyResult = await handler.Handle("");
var markerResult = await handler.Handle("@user");
var ordinaryResult = await handler.Handle("https://example.invalid/video");
Console.WriteLine($"Empty URL returns false: {!emptyResult}");
Console.WriteLine($"Existing @ prefix returns false: {!markerResult}");
Console.WriteLine($"Ordinary URL continues: {ordinaryResult}");
var passed = !emptyResult && !markerResult && ordinaryResult;
Console.WriteLine(passed
    ? "PASS: empty URL is rejected and existing prefix behavior is preserved"
    : "FAIL: extracted HandleUrl prefix returned an unexpected result");
Environment.ExitCode = passed ? 0 : 1;
