var onlyPinned = (ObservableCollection<CommentModel>)ls_new2.ItemsSource;
ok &= onlyPinned.Count == 1 && ReferenceEquals(onlyPinned[0], pin);
Console.WriteLine(ok ? "PASS: pin first; normal reply zero and nested model preserved; duplicate IDs suppressed; pinned-only insertion works" : "FAIL: pinned insertion assertions");
Environment.ExitCode = ok ? 0 : 1;
