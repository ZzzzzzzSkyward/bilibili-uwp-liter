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
