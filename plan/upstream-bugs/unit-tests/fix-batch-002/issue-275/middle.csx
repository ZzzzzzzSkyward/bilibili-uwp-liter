var shown = (ObservableCollection<CommentModel>)ls_new.ItemsSource;
bool ok = shown.Count == 2 && ReferenceEquals(shown[0], pin) && shown[0].showTop == Visibility.Visible && ReferenceEquals(shown[1], normal0) && ReferenceEquals(shown[1].children[0], nested);
var m2 = new M { data = new CommentData { topReplies = new List<CommentModel>{pin}, replies = new List<CommentModel>() } };
var ls_new2 = new Items();
m = m2; ls_new = ls_new2;
