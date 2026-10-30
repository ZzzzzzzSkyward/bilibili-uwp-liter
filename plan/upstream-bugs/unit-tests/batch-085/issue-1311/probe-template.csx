using System;

sealed class SubtitleItem
{
    public string lan_doc;
    public string lan;
    public string subtitle_url;
}

sealed class ToggleMenuFlyoutItem
{
    public string Text;
    public object Tag;
}

sealed class DownloadSubtitleInfo
{
    public string Name;
    public string Url;
}

ToggleMenuFlyoutItem CreateApiItem(SubtitleItem item)
{
    return new ToggleMenuFlyoutItem { __API_LABEL__ };
}

DownloadSubtitleInfo CreateDownloadItem(SubtitleItem subtitleItem)
{
    return new DownloadSubtitleInfo { __DOWNLOAD_LABEL__ };
}

bool SelectNone(string text, bool autoAISubtitle)
{
    var firstMenuItem = new ToggleMenuFlyoutItem { Text = text };
    __SELECT_NONE__
    return selectNone;
}

var aiTrack = new SubtitleItem { lan_doc = "English", lan = "ai-en", subtitle_url = "fixture" };
var apiMenuItem = CreateApiItem(aiTrack);
var downloaded = CreateDownloadItem(aiTrack);
var aiRejected = SelectNone(apiMenuItem.Text, false);
var manualAllowed = !SelectNone("English(zh-CN)", false);
var optedIn = !SelectNone(apiMenuItem.Text, true);
var labelsMatch = apiMenuItem.Text == "English(ai-en)" && downloaded.Name == apiMenuItem.Text;
Console.WriteLine($"API and newly downloaded subtitle labels retain language: {labelsMatch}");
Console.WriteLine($"Unmarked title with existing ai language marker is skipped when disabled: {aiRejected}");
Console.WriteLine($"Manual language remains selected when disabled: {manualAllowed}");
Console.WriteLine($"AI-marked subtitle remains selected when option is enabled: {optedIn}");
var passed = labelsMatch && aiRejected && manualAllowed && optedIn;
Console.WriteLine(passed
    ? "PASS: language labels restore the existing AI marker heuristic without suppressing manual tracks"
    : "FAIL: subtitle label or selection behavior diverged");
Environment.ExitCode = passed ? 0 : 1;
