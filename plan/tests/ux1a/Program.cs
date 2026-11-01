using System;
using System.IO;
using BiliLite.Models.Common;

namespace Ux1aProbe
{
    /// <summary>
    /// Ux1a: the page owns one sequence state, and the completion event, the player's next button
    /// and the next shortcut all resolve the same next target through it.
    ///
    /// Executed against product source: Models/WatchSequence.cs (the decision table and the state)
    /// and Models/MediaListResources.cs (MediaListQueue, the row/paging rule the sequence delegates
    /// to). Pages/VideoDetailPage and Controls/PlayerControl cannot run outside UWP, so their
    /// wiring is read as text; every such check is named "source:".
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            MidPartsAdvanceToTheNextPart();
            LastPartAdvancesIntoTheQueue();
            LastPartAdvancesInsideTheCollection();
            LastPartWithoutOuterSourceEnds();
            EmptyPartListUsesTheOuterSource();
            EmptyQueueWithMoreRowsDoesNotPage();
            FailedNextPageWaitsForRetry();
            DuplicateOnlyPageKeepsTheDecision();
            SkipRemainingPartsSkipsPartsOnly();
            PreviousWalksPartsThenRows();
            OnePartVideoInACollectionCanAdvance();
            IndexesStayInsideTheCounts();
            NextPartOnlyNeverLeavesTheVideo();
            KeyboardNextLeavesNoStaleIndex();
            StateFeedOrder();
            PageSourceWiring();
            PlayerSourceWiring();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void MidPartsAdvanceToTheNextPart()
        {
            // 三P视频的第二P,外面还有五行队列
            var position = Position(WatchSequenceSourceKind.Playlist, 1, 3, 0, 5);

            var step = WatchSequenceOrder.Next(position, false);
            Check(step.Move == WatchSequenceMove.PlayPart, "mid-parts next is a part move");
            Check(step.PartIndex == 2, "mid-parts next goes to the following part");
            Check(step.RowIndex == -1, "a part move carries no row");

            var skipped = WatchSequenceOrder.Next(position, true);
            Check(skipped.Move == WatchSequenceMove.PlayRow, "asking to skip the remaining parts leaves the video");
            Check(skipped.RowIndex == 1, "skipping from row 0 lands on row 1");
        }

        private static void LastPartAdvancesIntoTheQueue()
        {
            // 最后一P,队列里第 0 行(共 5 行)
            var position = Position(WatchSequenceSourceKind.Playlist, 2, 3, 0, 5);

            var step = WatchSequenceOrder.Next(position, false);
            Check(step.Move == WatchSequenceMove.PlayRow, "the last part advances into the playlist");
            Check(step.RowIndex == 1, "the playlist advances by exactly one row");
            Check(step.PartIndex == -1, "a row move carries no part");

            var atMiddleRow = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 3, 9), false);
            Check(atMiddleRow.Move == WatchSequenceMove.PlayRow && atMiddleRow.RowIndex == 4,
                "a one-part video in the middle of a queue advances to the next row");
        }

        private static void LastPartAdvancesInsideTheCollection()
        {
            // 单P视频,合集里第 3 集(共 10 集)
            var position = Position(WatchSequenceSourceKind.UgcSeason, 0, 1, 3, 10);

            var step = WatchSequenceOrder.Next(position, false);
            Check(step.Move == WatchSequenceMove.PlayRow, "the last part advances inside the collection");
            Check(step.RowIndex == 4, "the collection advances by exactly one row");

            var atEnd = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.UgcSeason, 0, 1, 9, 10), false);
            Check(atEnd.Move == WatchSequenceMove.End, "the last collection row ends the sequence");
        }

        private static void LastPartWithoutOuterSourceEnds()
        {
            var step = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.None, 2, 3, -1, 0), false);
            Check(step.Move == WatchSequenceMove.End, "no outer source ends after the last part");

            var standalone = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.None, 0, 1, -1, 0), false);
            Check(standalone.Move == WatchSequenceMove.End, "a standalone one-part video ends");

            var queuedButEmpty = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 0, 0), false);
            Check(queuedButEmpty.Move == WatchSequenceMove.End, "an active source without rows ends");
        }

        private static void EmptyPartListUsesTheOuterSource()
        {
            // 详情还没给出分P(PartCount 0):不能凭空造出一个分P目标
            var step = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 0, 1, 4), false);
            Check(step.Move == WatchSequenceMove.PlayRow, "an empty part list still follows the outer source");
            Check(step.RowIndex == 2, "an empty part list advances the outer row");

            Check(WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.None, 0, 0, -1, 0), false).Move == WatchSequenceMove.End,
                "an empty part list without an outer source ends");
        }

        private static void EmptyQueueWithMoreRowsDoesNotPage()
        {
            // 队列一行都没加载,但服务端还有页:没有"最后一行"可停,不能去翻页
            var step = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 0, 0, true, false), false);
            Check(step.Move == WatchSequenceMove.End, "an empty queue with more pages does not page");
        }

        private static void FailedNextPageWaitsForRetry()
        {
            var retry = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 4, 5, true, true), false);
            Check(retry.Move == WatchSequenceMove.RetryNextPage, "a failed next page asks for a retry, not a new request");

            var load = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 4, 5, true, false), false);
            Check(load.Move == WatchSequenceMove.LoadNextPage, "the last loaded row fetches the next page");

            var done = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 4, 5, false, true), false);
            Check(done.Move == WatchSequenceMove.End, "a failed page with nothing more to fetch ends instead of retrying forever");
        }

        private static void DuplicateOnlyPageKeepsTheDecision()
        {
            // 停在最后一行、还有下一页
            var state = new WatchSequenceState();
            state.SetActiveSource(WatchSequenceSourceKind.Playlist);
            state.SetParts(0, 1);
            state.SetRows(WatchSequenceSourceKind.Playlist, 2, 3, true, false);
            Check(WatchSequenceOrder.Next(state.Position, false).Move == WatchSequenceMove.LoadNextPage,
                "the last loaded row asks for the next page");

            // 取回来的页全是已加载的行:行数和行号都不动,态度变成"到底了"
            state.SetRows(WatchSequenceSourceKind.Playlist, 2, 3, false, false);
            Check(state.Position.RowIndex == 2, "a duplicate-only page keeps the current row");
            Check(state.Position.RowCount == 3, "a duplicate-only page does not invent rows");
            Check(WatchSequenceOrder.Next(state.Position, false).Move == WatchSequenceMove.End,
                "a duplicate-only page stops instead of paging forever");

            // 取回来的页有新行:同一个行号直接走下一行
            state.SetRows(WatchSequenceSourceKind.Playlist, 2, 5, false, false);
            var step = WatchSequenceOrder.Next(state.Position, false);
            Check(step.Move == WatchSequenceMove.PlayRow && step.RowIndex == 3,
                "rows appended by the next page are played from the same position");
        }

        private static void SkipRemainingPartsSkipsPartsOnly()
        {
            // 三P视频的第 1P(下标 1),队列第 0 行
            var position = Position(WatchSequenceSourceKind.Playlist, 1, 3, 0, 3);

            Check(WatchSequenceOrder.NextPartOnly(position).PartIndex == 2, "next-part-only still walks the parts");

            var step = WatchSequenceOrder.Next(position, true);
            Check(step.Move == WatchSequenceMove.PlayRow && step.RowIndex == 1,
                "skipping the remaining parts goes straight to the outer row");

            var paging = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 1, 3, 2, 3, true, false), true);
            Check(paging.Move == WatchSequenceMove.LoadNextPage,
                "skipRemainingParts never skips the paging step");

            var retrying = WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 1, 3, 2, 3, true, true), true);
            Check(retrying.Move == WatchSequenceMove.RetryNextPage,
                "skipRemainingParts still waits for the failed page retry");
        }

        private static void PreviousWalksPartsThenRows()
        {
            var part = WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.Playlist, 2, 3, 1, 5));
            Check(part.Move == WatchSequenceMove.PlayPart && part.PartIndex == 1, "previous inside the video walks the parts");

            var row = WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.Playlist, 0, 1, 3, 5));
            Check(row.Move == WatchSequenceMove.PlayRow && row.RowIndex == 2,
                "at the first part previous walks the outer row");

            var collectonRow = WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.UgcSeason, 0, 2, 1, 4));
            Check(collectonRow.Move == WatchSequenceMove.PlayRow && collectonRow.RowIndex == 0,
                "at the first part previous walks the collection");

            Check(WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.Playlist, 0, 1, 0, 5)).Move == WatchSequenceMove.None,
                "the first row has no previous");
            Check(WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.None, 0, 1, -1, 0)).Move == WatchSequenceMove.None,
                "without an outer source the first part has no previous");
            Check(WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.UgcSeason, 0, 1, -1, 4)).Move == WatchSequenceMove.None,
                "a video outside the collection has no previous row");
            Check(WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.Playlist, 0, 1, 0, 5, true, false)).Move == WatchSequenceMove.None,
                "previous never pages backwards");

            var firstPartOfMany = WatchSequenceOrder.Previous(Position(WatchSequenceSourceKind.None, 0, 3, -1, 0));
            Check(firstPartOfMany.Move == WatchSequenceMove.None, "the first part of a standalone video has no previous");
        }

        private static void OnePartVideoInACollectionCanAdvance()
        {
            // 一P的合集视频:旧代码里"下一P"按钮根本不出现,手动也没法往下走
            var position = Position(WatchSequenceSourceKind.UgcSeason, 0, 1, 0, 3);

            Check(WatchSequenceOrder.NextPartOnly(position).Move == WatchSequenceMove.None,
                "a one-part video has no next part");

            var step = WatchSequenceOrder.Next(position, false);
            Check(step.Move == WatchSequenceMove.PlayRow && step.RowIndex == 1,
                "a one-part collection video advances to the next episode");
            Check(WatchSequenceOrder.Next(position, true).Move == WatchSequenceMove.PlayRow,
                "skipping parts on a one-part collection video is the same row step");
        }

        private static void IndexesStayInsideTheCounts()
        {
            var sources = new[] { WatchSequenceSourceKind.None, WatchSequenceSourceKind.UgcSeason, WatchSequenceSourceKind.Playlist };

            foreach (var source in sources)
            {
                for (var rowCount = 0; rowCount <= 4; rowCount++)
                {
                    for (var rowIndex = -2; rowIndex <= rowCount + 2; rowIndex++)
                    {
                        foreach (var hasMore in new[] { false, true })
                        {
                            foreach (var failed in new[] { false, true })
                            {
                                var position = Position(source, 1, 3, rowIndex, rowCount, hasMore, failed);
                                var next = WatchSequenceOrder.Next(position, false);
                                var previous = WatchSequenceOrder.Previous(position);

                                foreach (var step in new[] { next, previous })
                                {
                                    if (step.Move == WatchSequenceMove.PlayRow)
                                    {
                                        Check(step.RowIndex >= 0 && step.RowIndex < rowCount,
                                            "a row move stays inside the loaded rows (" + source + " " + rowIndex + "/" + rowCount + ")");
                                    }
                                    if (step.Move == WatchSequenceMove.PlayPart)
                                    {
                                        Check(step.PartIndex >= 0 && step.PartIndex < position.PartCount,
                                            "a part move stays inside the parts");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 停在最后一行、还有下一页:只会去翻页,不会跳到最后一行之外的行
            Check(WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 1, 4, 5, true, false), false).Move
                == WatchSequenceMove.LoadNextPage, "the last row never produces an out-of-range row move");

            foreach (var partIndex in new[] { -3, -1, 0, 1, 2, 3, 7 })
            {
                var position = Position(WatchSequenceSourceKind.None, partIndex, 3, -1, 0);
                var step = WatchSequenceOrder.Next(position, false);
                if (step.Move != WatchSequenceMove.PlayPart) continue;
                Check(step.PartIndex >= 0 && step.PartIndex < 3, "a part index outside the video never becomes a target");
            }

            Check(WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.None, -1, 3, -1, 0), false).Move
                == WatchSequenceMove.End, "a negative part index does not produce part 0 as a target");
        }

        private static void NextPartOnlyNeverLeavesTheVideo()
        {
            Check(WatchSequenceOrder.NextPartOnly(Position(WatchSequenceSourceKind.Playlist, 0, 3, 0, 5)).Move
                == WatchSequenceMove.PlayPart, "next-part-only walks the parts");
            Check(WatchSequenceOrder.NextPartOnly(Position(WatchSequenceSourceKind.Playlist, 2, 3, 0, 5)).Move
                == WatchSequenceMove.None, "next-part-only stops at the last part and never touches the queue");
            Check(WatchSequenceOrder.NextPartOnly(Position(WatchSequenceSourceKind.Playlist, 0, 0, 0, 5)).Move
                == WatchSequenceMove.None, "next-part-only has nothing to do without parts");
            Check(WatchSequenceOrder.NextPartOnly(null).Move == WatchSequenceMove.None, "no position has no next part");
        }

        private static void KeyboardNextLeavesNoStaleIndex()
        {
            // 两分P视频外面还有五行队列:键盘从 P1 换到 P2 之后,播放器的当前下标是 1,
            // 于是序列判定和"下一个"按钮都必须承认本视频的分P已经走完
            var atSecondPart = Position(WatchSequenceSourceKind.Playlist, 1, 2, 0, 5);
            var step = WatchSequenceOrder.Next(atSecondPart, false);
            Check(step.Move == WatchSequenceMove.PlayRow && step.RowIndex == 1,
                "the second part of two is the last part, so next leaves the video for the queue row");
            Check(WatchSequenceOrder.Next(Position(WatchSequenceSourceKind.Playlist, 0, 2, 0, 5), false).Move
                == WatchSequenceMove.PlayPart, "the first part of two still advances to the following part");
        }

        private static void StateFeedOrder()
        {
            var state = new WatchSequenceState();
            Check(state.Position.Source == WatchSequenceSourceKind.None && state.Position.PartCount == 0,
                "a fresh state has no source and no parts");

            state.SetParts(1, 3);
            Check(state.Position.PartIndex == 1 && state.Position.PartCount == 3, "parts are fed independently of the source");
            Check(WatchSequenceOrder.Next(state.Position, false).Move == WatchSequenceMove.PlayPart,
                "feeding parts is enough for a part step");

            state.SetActiveSource(WatchSequenceSourceKind.Playlist);
            state.SetRows(WatchSequenceSourceKind.Playlist, 2, 6, true, false);
            Check(state.Position.Source == WatchSequenceSourceKind.Playlist && state.Position.RowCount == 6,
                "rows carry their own source");
            Check(state.Position.PartIndex == 1 && state.Position.PartCount == 3, "feeding rows keeps the parts");

            state.SetParts(2, 3);
            Check(state.Position.RowIndex == 2 && state.Position.HasMoreRows,
                "feeding parts keeps the rows");

            // 换来源:上一个来源的行不能留着影响新来源
            state.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
            Check(state.Position.RowIndex == -1 && state.Position.RowCount == 0 && !state.Position.HasMoreRows,
                "switching the source drops the previous source's rows");

            state.SetActiveSource(WatchSequenceSourceKind.UgcSeason);
            Check(state.Position.PartIndex == 2, "re-setting the same source keeps the parts");

            state.SetRows(WatchSequenceSourceKind.UgcSeason, 0, 10, false, false);
            Check(WatchSequenceOrder.Next(state.Position, false).Move == WatchSequenceMove.PlayRow,
                "a collection beyond the last part advances to the next episode");

            state.Clear();
            Check(state.Position.Source == WatchSequenceSourceKind.None && state.Position.PartCount == 0
                && state.Position.RowCount == 0, "clearing a state leaves it fresh");
        }

        private static void PageSourceWiring()
        {
            var page = ReadSource("src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs");

            Check(page.Contains("readonly WatchSequenceState sequence = new WatchSequenceState();"),
                "source: the page owns one sequence state");
            Check(!page.Contains("playFromUgcSeason"),
                "source: the former collection/queue bool is gone, the state is the only source record");
            Check(!page.Contains("MediaListQueue.NextAction("),
                "source: the page does not keep a second copy of the row rule");

            Check(Body(page, "private async void player_AllMediaEndEvent").Contains("await AdvanceSequenceAsync(false, false);"),
                "source: the completion event resolves through the page's sequence method");
            Check(Body(page, "private async void player_NextOuterRequested").Contains("await AdvanceSequenceAsync(false, true);"),
                "source: the next button's request resolves through the same method");
            Check(Body(page, "private async void player_NextVideoRequested").Contains("await AdvanceSequenceAsync(true, true);"),
                "source: the skip-remaining-parts request resolves through the same method");
            Check(Body(page, "private async void player_PreviousOuterRequested")
                    .Contains("WatchSequenceOrder.Previous(sequence.Position)"),
                "source: the previous request resolves through the same state");

            Check(CountOf(page, "private async Task<bool> AdvanceSequenceAsync(") == 1,
                "source: there is exactly one advance path in the page");
            Check(CountOf(page, "await AdvanceSequenceAsync(") == 3,
                "source: completion, next and skip-remaining-parts all call it");

            Check(Body(page, "private async Task<bool> AdvanceSequenceAsync").Contains("WatchSequenceOrder.Next(sequence.Position, skipRemainingParts)"),
                "source: the advance path asks the sequence for the target");
            Check(Body(page, "private async Task<bool> AdvanceSequenceAsync").Contains("player.ChangePlayIndex(step.PartIndex, PlayTargetStart.FromStart);"),
                "source: a part move uses the player's own part switch");
            Check(Body(page, "private async Task<bool> AdvanceSequenceAsync").Contains("await PlaySequenceRowAsync(step.RowIndex, PlayTargetStart.FromStart);"),
                "source: a row move uses the row executor");
            Check(Body(page, "private async Task<bool> AdvanceSequenceAsync").Contains("if (await LoadMediaListPage())"),
                "source: a page step fetches through the existing page loader");
            Check(Body(page, "private async Task<bool> AdvanceSequenceAsync").Contains("if (_isClosed) return false;")
                && Body(page, "private async Task<bool> AdvanceSequenceAsync").Contains("if (flag) return false;"),
                "source: the closed-page and in-flight guards stay on the advance path");
            Check(Body(page, "private async Task PlayCollectionRowAsync").Contains("SelectUgcSeasonRow(listView, row);"),
                "source: a collection row move reuses the existing selection handler");
            Check(Body(page, "private async Task PlayCollectionRowAsync").Contains("await InitializeVideo(aid, start);"),
                "source: a collection row that is already highlighted still starts that video");

            Check(page.Contains("sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);"),
                "source: picking a collection row makes the collection the active source");
            Check(page.Contains("sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);"),
                "source: picking a queue row makes the queue the active source");
            Check(Body(page, "private void ApplyDefaultSequenceSource")
                    .Contains("if (sequence.Position.Source != WatchSequenceSourceKind.None) return;")
                && Body(page, "private void ApplyDefaultSequenceSource")
                    .Contains("sequence.SetActiveSource(WatchSequenceSourceKind.UgcSeason);"),
                "source: the collection is only the fallback when no queue and no row click chose a source");
            Check(page.Contains("sequence.SetActiveSource(WatchSequenceSourceKind.Playlist);")
                && page.Contains("if (e.Parameter is VideoPlaylist)"),
                "source: opening through a queue keeps the queue active");

            Check(page.Contains("player.NextOuterRequested += player_NextOuterRequested;")
                && page.Contains("player.NextVideoRequested += player_NextVideoRequested;")
                && page.Contains("player.PreviousOuterRequested += player_PreviousOuterRequested;"),
                "source: the page subscribes to all three player requests");

            Check(Body(page, "private void RefreshSequenceState").Contains("VideoUgcSeasonOrder.AidOrder("),
                "source: collection rows come from the existing section/episode order");
            Check(Body(page, "private void RefreshSequenceState").Contains("videoDetailVM?.VideoInfo?.aid"),
                "source: the current collection row is the position of the current aid");
            Check(Body(page, "private void RefreshSequenceState").Contains("UpdateSequenceInfo("),
                "source: the page pushes the next/previous preview to the player");
            Check(Body(page, "private void RefreshSequenceState").Contains("mediaListState != null && mediaListState.HasMore,")
                && Body(page, "private void RefreshSequenceState").Contains("mediaListState != null && mediaListState.LastLoadFailed);"),
                "source: the row paging flags come from the paging state");

            Check(Body(page, "private string DescribeStep").Contains("return PartTitle(step.PartIndex);")
                && Body(page, "private string DescribeStep").Contains("return RowTitle(step.RowIndex);")
                && Body(page, "private string DescribeStep").Contains("return \"下一页\";"),
                "source: the preview is the bare target: part title, row title, or 下一页");
            Check(!page.Contains("下一P：") && !page.Contains("下一个视频：") && !page.Contains("下一集：")
                && !page.Contains("上一P：") && !page.Contains("上一集：") && !page.Contains("RowPrefix"),
                "source: the preview no longer prefixes its direction or explains a retry");

            Check(Body(page, "private bool CanSkipToNextVideo").Contains("position.RowCount > 0")
                && Body(page, "private bool CanSkipToNextVideo").Contains("position.PartIndex < position.PartCount - 1"),
                "source: skipping is offered only with an outer source and a remaining part");

            Check(!page.Contains("private async Task<bool> AdvanceUgcSeasonAsync()")
                && !page.Contains("private async Task AdvancePlaylistAsync()"),
                "source: the two old independent advance paths are gone");
        }

        private static void PlayerSourceWiring()
        {
            var player = ReadSource("src/BiliLite.UWP/Controls/PlayerControl.xaml.cs");

            Check(player.Contains("public event EventHandler NextOuterRequested;")
                && player.Contains("public event EventHandler PreviousOuterRequested;")
                && player.Contains("public event EventHandler NextVideoRequested;"),
                "source: the player exposes the three sequence requests");
            Check(player.Contains("public void UpdateSequenceInfo(string nextTargetText, string previousTargetText, bool canSkipToNextVideo)"),
                "source: the player exposes the reported target text");

            Check(!player.Contains("已经是最后一P了"), "source: the player no longer dead-ends the last part with a toast");
            Check(!player.Contains("已经是第一P了"), "source: the player no longer dead-ends the first part with a toast");
            Check(player.Contains("NextOuterRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the player raises the next request instead");
            Check(player.Contains("PreviousOuterRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the player raises the previous request instead");
            Check(player.Contains("NextVideoRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the player raises the skip-remaining-parts request");

            var nextShortcut = Body(player, "case Windows.System.VirtualKey.X:");
            Check(nextShortcut.Contains("if (HasNextPart)") && nextShortcut.Contains("await SetPlayItem(CurrentPlayIndex + 1, PlayTargetStart.FromStart);")
                && nextShortcut.Contains("NextOuterRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the next shortcut walks parts on the current index and asks the page at the last one");
            Check(!nextShortcut.Contains("EpisodeList"),
                "source: the next shortcut no longer branches on the list selection");
            var previousShortcut = Body(player, "case Windows.System.VirtualKey.Z:");
            Check(previousShortcut.Contains("if (HasPreviousPart)")
                && previousShortcut.Contains("await SetPlayItem(CurrentPlayIndex - 1, PlayTargetStart.Resume);")
                && previousShortcut.Contains("PreviousOuterRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the previous shortcut walks parts on the current index and asks the page at the first one");
            Check(!previousShortcut.Contains("EpisodeList"),
                "source: the previous shortcut no longer branches on the list selection");

            var nextClick = Body(player, "private async void BottomBtnNext_Click");
            Check(nextClick.Contains("if (HasNextPart)") && nextClick.Contains("await SetPlayItem(CurrentPlayIndex + 1, PlayTargetStart.FromStart);")
                && nextClick.Contains("NextOuterRequested?.Invoke(this, EventArgs.Empty);"),
                "source: the next button walks parts on the current index and asks the page at the last one");
            Check(!nextClick.Contains("EpisodeList"),
                "source: the next button does not read the list selection at all");

            Check(Body(player, "private bool HasNextPart").Contains("CurrentPlayIndex < PlayInfos.Count - 1")
                && Body(player, "private bool HasPreviousPart").Contains("CurrentPlayIndex > 0"),
                "source: both part boundary rules read the playing index");

            var setItem = Body(player, "private async Task SetPlayItem");
            Check(setItem.Contains("CurrentPlayIndex = index;") && setItem.Contains("SyncEpisodeListSelection(index);"),
                "source: the played part is assigned and the list selection mirrors it");
            Check(CountOf(player, "CurrentPlayIndex = index;") == 1,
                "source: exactly one place assigns the playing index");

            var sync = Body(player, "private void SyncEpisodeListSelection");
            Check(sync.Contains("if (EpisodeList.SelectedIndex == index)")
                && sync.Contains("_syncingEpisodeSelection = true;") && sync.Contains("finally")
                && sync.Contains("_syncingEpisodeSelection = false;"),
                "source: the list selection is synced under a re-entrancy guard that is always released");

            var changed = Body(player, "private async void EpisodeList_SelectionChanged");
            Check(changed.Contains("if (_syncingEpisodeSelection)") && changed.Contains("return;"),
                "source: the selection event raised by the sync does not start a second playback");
            Check(changed.Contains("await SetPlayItem(EpisodeList.SelectedIndex, PlayTargetStart.Resume);"),
                "source: a user picking a part still goes through the same part switch");

            Check(CountOf(player, "EpisodeList.SelectedIndex = index;") == 1
                && Body(player, "public void InitializePlayInfo").Contains("_ = SetPlayItem(index, start);")
                && sync.Contains("EpisodeList.SelectedIndex = index;"),
                "source: the list selection is written only inside the mirror guard");

            var changeIndex = Body(player, "public void ChangePlayIndex");
            Check(changeIndex.Contains("_ = SetPlayItem(index, start);") && !changeIndex.Contains("EpisodeList.SelectedIndex"),
                "source: a page-requested part change uses the same part switch as the shortcut");
            Check(changeIndex.Contains("if (index == CurrentPlayIndex)"),
                "source: a page request for the part already playing does not restart it");

            var update = Body(player, "public void UpdateSequenceInfo");
            Check(update.Contains("ToolTipService.SetToolTip(BottomBtnNext, nextName);")
                && update.Contains("AutomationProperties.SetName(BottomBtnNext, nextName);"),
                "source: the reported target names the next button for pointer and screen reader users");

            var visibility = Body(player, "private void UpdateBottomBtnNextVisibility");
            Check(visibility.Contains("HasNextPart || _hasOuterNextTarget"),
                "source: the next button stays when a part remains or the page reported an outer target");
            Check(Body(player, "private async Task SetPlayItem").Contains("UpdateBottomBtnNextVisibility();"),
                "source: the shared visibility rule is used when the played part changes");

            Check(!player.Contains("LoadMediaListPage") && !player.Contains("InitializeVideo("),
                "source: the player never fetches pages or starts a video itself");
        }

        private static WatchSequencePosition Position(WatchSequenceSourceKind source, int partIndex, int partCount,
            int rowIndex, int rowCount, bool hasMoreRows = false, bool lastRowPageFailed = false)
        {
            return new WatchSequencePosition(source, partIndex, partCount, rowIndex, rowCount, hasMoreRows, lastRowPageFailed);
        }

        private static string Body(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            var length = Math.Min(4000, source.Length - start);
            var body = source.Substring(start, length);
            var end = body.IndexOf("\n        }\n", StringComparison.Ordinal);
            return end < 0 ? body : body.Substring(0, end);
        }

        private static int CountOf(string source, string text)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(text, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += text.Length;
            }
            return count;
        }

        private static string ReadSource(string relativePath)
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            //仓库里是 CRLF,而 Body 用 "\n        }" 找方法结尾:先把行尾统一,否则只会截到 4000 字符
            return text.Replace("\r\n", "\n");
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Models", "WatchSequence.cs")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("repository root not found from " + AppContext.BaseDirectory);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL " + message);
                Environment.Exit(1);
            }
            _passed++;
            Console.WriteLine("ok " + message);
        }
    }
}
