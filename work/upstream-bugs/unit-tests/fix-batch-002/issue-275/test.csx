using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
enum Visibility { Visible, Collapsed }
sealed class CommentModel { public long rpid; public Visibility showTop; public string payload; public List<CommentModel> children = new(); }
sealed class CommentData { public CommentData upper; public CommentModel top; public List<CommentModel> topReplies; public List<CommentModel> replies; }
sealed class M { public CommentData data; }
sealed class Items { public object ItemsSource; }
var pin = new CommentModel { rpid=10, payload="pinned" };
var duplicate = new CommentModel { rpid=10, payload="duplicate" };
var normal0 = new CommentModel { rpid=20, payload="normal0" };
var nested = new CommentModel { rpid=21, payload="nested" };
normal0.children.Add(nested);
var m = new M { data = new CommentData { topReplies = new List<CommentModel>{pin, duplicate}, replies = new List<CommentModel>{normal0, new CommentModel{rpid=10}} } };
var ls_new = new Items();
{
                                var displayedReplies = new ObservableCollection<CommentModel>();
                                if (m.data.upper != null && m.data.upper.top != null)
                                {
                                    m.data.upper.top.showTop = Visibility.Visible;
                                    displayedReplies.Add(m.data.upper.top);
                                }
                                if (m.data.topReplies != null)
                                {
                                    foreach (var item in m.data.topReplies)
                                    {
                                        item.showTop = Visibility.Visible;
                                        if (!displayedReplies.Any(reply => reply.rpid == item.rpid))
                                        {
                                            displayedReplies.Add(item);
                                        }
                                    }
                                }
                                foreach (var item in m.data.replies)
                                {
                                    if (!displayedReplies.Any(reply => reply.rpid == item.rpid))
                                    {
                                        displayedReplies.Add(item);
                                    }
                                }
                                ls_new.ItemsSource = displayedReplies;
}
var shown = (ObservableCollection<CommentModel>)ls_new.ItemsSource;
bool ok = shown.Count == 2 && ReferenceEquals(shown[0], pin) && shown[0].showTop == Visibility.Visible && ReferenceEquals(shown[1], normal0) && ReferenceEquals(shown[1].children[0], nested);
var m2 = new M { data = new CommentData { topReplies = new List<CommentModel>{pin}, replies = new List<CommentModel>() } };
var ls_new2 = new Items();
m = m2; ls_new = ls_new2;
{
                                var displayedReplies = new ObservableCollection<CommentModel>();
                                if (m.data.upper != null && m.data.upper.top != null)
                                {
                                    m.data.upper.top.showTop = Visibility.Visible;
                                    displayedReplies.Add(m.data.upper.top);
                                }
                                if (m.data.topReplies != null)
                                {
                                    foreach (var item in m.data.topReplies)
                                    {
                                        item.showTop = Visibility.Visible;
                                        if (!displayedReplies.Any(reply => reply.rpid == item.rpid))
                                        {
                                            displayedReplies.Add(item);
                                        }
                                    }
                                }
                                foreach (var item in m.data.replies)
                                {
                                    if (!displayedReplies.Any(reply => reply.rpid == item.rpid))
                                    {
                                        displayedReplies.Add(item);
                                    }
                                }
                                ls_new.ItemsSource = displayedReplies;
}
var onlyPinned = (ObservableCollection<CommentModel>)ls_new2.ItemsSource;
ok &= onlyPinned.Count == 1 && ReferenceEquals(onlyPinned[0], pin);
Console.WriteLine(ok ? "PASS: pin first; normal reply zero and nested model preserved; duplicate IDs suppressed; pinned-only insertion works" : "FAIL: pinned insertion assertions");
Environment.ExitCode = ok ? 0 : 1;
