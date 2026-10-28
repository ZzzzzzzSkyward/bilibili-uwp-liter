                            cursor=m.data.cursor;
                            if (m.data.replies.Count >= 10 && (!isCursorApiResponse || cursor?.PaginationReply?.NextOffset != null))
                            {
                                btn_LoadMore.Visibility = Visibility.Visible;
                            }
