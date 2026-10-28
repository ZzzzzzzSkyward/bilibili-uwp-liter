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
