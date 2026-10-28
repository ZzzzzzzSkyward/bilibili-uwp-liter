# User-owned visual issue review

The original **59** visual assignments are preserved below. Current total: **78**, including [19 later assignments](USER_VISUAL_REVIEW_ADDITIONS.md). Functional portions of mixed reports have completed assessment; current results are in [assessment-final.json](assessment-final.json).

These are local catalog assignments only; no upstream issue was edited. Records are sorted by creation time.

## #51 — 搜索建议会盖住输入法
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/51
- Created: 2023-04-16T23:54:57Z
- Owner: user
- Visual subitem: Search suggestions are drawn above and obscure the IME candidate list.
- Observable: While typing in search, the suggestion panel covers the input method’s candidate text.
- Reason: The report says the suggestion popup covers text in the input-method list; it identifies a visual z-order collision and does not report that text entry or candidate selection fails.

## #62 — 4.6.0.1 弹幕会溢出视频范围，全屏界面上方有白条，多个选项卡间没有分割线
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/62
- Created: 2023-04-29T14:31:07Z
- Owner: user
- Visual subitem: Danmaku spill outside the video bounds; a white strip appears at the top of full screen; separators are missing between tabs.
- Observable: Danmaku crosses the video edge; full screen has a white top strip, and tabs have no divider.
- Reason: The report explicitly describes visible overflow and interface decoration/alignment defects.

## #65 — 修复4.6.0.1视频弹幕溢出问题
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/65
- Created: 2023-04-30T14:29:28Z
- Owner: user
- Visual subitem: Video danmaku overflow beyond the video bounds.
- Observable: Danmaku extend outside the video area.
- Reason: The report explicitly describes a visual overflow/clipping defect and confirms its cause; the fix suggestion does not make it a nonvisual logic failure.

## #69 — 在动态页面查看评论时点击图片会出现在评论窗口下层
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/69
- Created: 2023-05-02T17:58:28Z
- Owner: user
- Visual subitem: Comment images are layered behind the dynamic comment panel.
- Observable: A selected image is hidden beneath the comment window.
- Reason: This is a clear z-order/overlap defect in the rendered interface.

## #70 — 部分按钮图标位置没有居中
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/70
- Created: 2023-05-03T15:02:11Z
- Owner: user
- Visual subitem: Some toolbar icons are not centered in their buttons.
- Observable: Icons appear off-center in the dynamic-comment and image-viewer toolbars.
- Reason: This is a direct icon-placement defect.

## #77 — 播放器页面响应式布局
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/77
- Created: 2023-05-06T08:20:16Z
- Owner: user
- Visual subitem: Player controls are obscured at narrow window widths.
- Observable: At a small window width, controls on the right side are hidden.
- Reason: Controls are visibly clipped by the responsive layout.

## #81 — CDN 测速后直接展开显示不全
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/81
- Created: 2023-05-07T08:41:31Z
- Owner: user
- Visual subitem: The expanded CDN results area initially cuts off content and cannot scroll.
- Observable: The first expansion shows only part of the CDN test results; reopening fixes the view.
- Reason: The report describes a visible clipping/scroll-layout failure.

## #83 — 这分P实现是不是能改改，现在显示不全
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/83
- Created: 2023-05-07T08:51:44Z
- Owner: user
- Visual subitem: Long part titles are not fully visible in the part list.
- Observable: Part titles are cut off in the part selector.
- Reason: The report is a truncation/display issue, also described by the related #550 report.

## #88 — 评论区图片默认缩放问题
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/88
- Created: 2023-05-08T05:36:12Z
- Owner: user
- Visual subitem: Comment images are zoomed so far that the full image cannot be seen by default.
- Observable: An image opens cropped/zoomed and requires manual zooming out.
- Reason: The image viewer’s default scaling is a clear presentation/layout defect.

## #89 — 更新后无法加载合集视频，上下按钮是乱码
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/89
- Created: 2023-05-08T14:27:15Z
- Owner: user
- Visual subitem: The up/down scroll-button label renders as garbled text.
- Observable: The collection navigation button shows unreadable characters.
- Reason: This is a visible text-rendering defect; the separate collection-playback failure remains for logic review.
- Pending logic subitems: The collection video itself also fails to load.

## #95 — 关于标签栏的优化建议
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/95
- Created: 2023-05-10T11:49:20Z
- Owner: user
- Visual subitem: Long tab titles are truncated.
- Observable: Only the beginning of a long video title is visible in its tab.
- Reason: This is a direct clipping defect in the tab layout.

## #96 — 4.60版皮肤问题、BUG
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/96
- Created: 2023-05-11T13:05:05Z
- Owner: user
- Visual subitem: The default skin and light-theme background render with incorrect colors.
- Observable: The update makes the default skin stark white and switching from dark to light leaves a gray background.
- Reason: Theme colors are appearance defects.

## #100 — 单窗口显示模式下，点击关注的UP主页，存在重叠Bug
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/100
- Created: 2023-05-16T03:08:10Z
- Owner: user
- Visual subitem: The followed-user list and profile page overlap in single-window mode.
- Observable: The followed-user list remains drawn over the creator profile.
- Reason: The report describes a visible page-layout overlap.

## #102 — 低宽度情况下评论区部分元素会冲突
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/102
- Created: 2023-05-18T02:53:54Z
- Owner: user
- Visual subitem: Comment-area elements collide at narrow widths.
- Observable: Comment controls/text overlap when the comment pane is narrow.
- Reason: The reported low-width element collision is a clear layout defect.

## #131 — 点赞按钮问题
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/131
- Created: 2023-05-25T09:56:00Z
- Owner: user
- Visual subitem: The liked-state pink color disappears when the pointer hovers over the like button.
- Observable: A liked video’s button loses its pink color until the view is refreshed.
- Reason: The visual state indicator is rendered inconsistently on hover.

## #144 — 侧键关闭页面后鼠标消失
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/144
- Created: 2023-05-30T15:13:43Z
- Owner: user
- Visual subitem: The mouse pointer remains invisible after closing a video with a side button.
- Observable: After the described close action, the pointer stays hidden on the home page.
- Reason: The reported defect is the visibility of the pointer after navigation.

## #159 — CDN 一栏更新后怎么感觉变糊了？是不是 DPI 缩放出问题了
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/159
- Created: 2023-06-11T05:36:30Z
- Owner: user
- Visual subitem: Expanded CDN rows render blurry while collapsed rows look normal.
- Observable: Text becomes blurry only after expanding a CDN row.
- Reason: This is a clear DPI/text-rendering appearance defect.

## #192 — 在最新版中吧显示模式改为单窗口模式的bug
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/192
- Created: 2023-07-07T02:01:17Z
- Owner: user
- Visual subitem: Video pages visually overlap when switching videos in single-window mode.
- Observable: The next video page is drawn over the previous one.
- Reason: The report identifies a rendered page-overlap defect.

## #199 — 单窗口模式全屏弹幕显示错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/199
- Created: 2023-07-11T12:04:50Z
- Owner: user
- Visual subitem: Full-screen danmaku is positioned beyond the video’s vertical bounds.
- Observable: Danmaku extends slightly above the video image.
- Reason: This is a clear placement/containment defect in the overlay.

## #299 — 视频亮度问题
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/299
- Created: 2023-09-03T14:43:14Z
- Owner: user
- Visual subitem: The brightness control produces a visibly incorrect image brightness.
- Observable: The same video looks markedly darker in the app than on the web at comparable brightness settings.
- Reason: This is an appearance defect in the rendered picture.

## #353 — 单窗口模式-搜索透明背景
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/353
- Created: 2023-10-23T03:03:27Z
- Owner: user
- Visual subitem: The description tab background becomes transparent after playing a searched video in single-window mode.
- Observable: The description view turns transparent after the reported navigation sequence.
- Reason: This is a clear background-rendering defect.

## #439 — 视频评论区的黄豆表情似乎有错位现象
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/439
- Created: 2023-12-18T06:03:25Z
- Owner: user
- Visual subitem: The yellow-bean emoji is vertically misaligned in some comments.
- Observable: Standalone yellow-bean emoji sit too high relative to adjacent text.
- Reason: This is a direct inline-image placement defect.

## #460 — 番剧详情页样式错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/460
- Created: 2023-12-27T12:29:13Z
- Owner: user
- Visual subitem: Anime-detail button text colors do not adapt to the theme and become unreadable.
- Observable: The right-side button labels are not visible against the selected theme.
- Reason: This is a color/contrast presentation defect.

## #473 — 单窗口模式全屏播放视频顶部弹幕被截断
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/473
- Created: 2024-01-05T04:36:36Z
- Owner: user
- Visual subitem: The top row of danmaku is clipped in full-screen single-window mode.
- Observable: The topmost danmaku line is cut off in full screen.
- Reason: This is a clear overlay clipping defect.

## #530 — 播放视频暂停后左上角会出现内部透明的白色实线方框
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/530
- Created: 2024-03-19T11:11:43Z
- Owner: user
- Visual subitem: Pausing in full screen draws an unwanted white outlined rectangle at the upper-left.
- Observable: A transparent white rectangle flashes or remains after the first pause.
- Reason: This is a transient visual artifact with no reported functional failure.

## #549 — 选中文字后右键菜单全选按钮缺少图标
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/549
- Created: 2024-03-27T14:19:51Z
- Owner: user
- Visual subitem: The “Select all” context-menu command is missing its icon.
- Observable: The Select all menu item appears without its expected icon.
- Reason: The missing icon is a cosmetic subitem; the separate intermittent context-menu failure remains for logic review.
- Pending logic subitems: The context menu sometimes fails to open after text selection.

## #562 — 个人主页动态修复后出现的相关bug
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/562
- Created: 2024-04-09T09:03:52Z
- Owner: user
- Visual subitem: Some dynamic images are not scaled to their intended dimensions.
- Observable: Some feed images appear at incorrect dimensions or proportions.
- Reason: The image-sizing problem is a clear layout subitem; missing dynamic text/article content and the wrong comment-panel type remain pending logic review.
- Pending logic subitems: Some dynamics omit text/article content; the dynamic comment area opens as a video-comment panel.

## #583 — Windows10下深色模式启动页面背景并非深色
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/583
- Created: 2024-04-14T07:58:03Z
- Owner: user
- Visual subitem: On Windows 10, the startup background follows the system accent color instead of the dark theme.
- Observable: The splash screen is bright orange rather than dark when Windows 10 is in dark mode.
- Reason: This is a clear theme/background appearance defect.

## #609 — 弹幕字体显示异常
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/609
- Created: 2024-04-29T12:42:54Z
- Owner: user
- Visual subitem: Live danmaku fonts render abnormally on the reported Windows 11 build.
- Observable: Danmaku characters have visibly abnormal font rendering.
- Reason: This is a direct typography/rendering defect.

## #658 — 点赞收藏投币按钮颜色错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/658
- Created: 2024-06-08T03:13:04Z
- Owner: user
- Visual subitem: Like/favorite/coin buttons lose their selected color on hover.
- Observable: A selected action button returns to its default color when hovered.
- Reason: This is a clear color/state-indicator rendering defect.

## #716 — 内容过滤设置规则项在Win10中样式错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/716
- Created: 2024-07-20T15:00:30Z
- Owner: user
- Visual subitem: On Windows 10, an unexpanded content-filter rule row is narrower than its expanded layout.
- Observable: The rule row changes width when expanded.
- Reason: This is a clear width/alignment defect in the settings UI.

## #799 — 超过100集以上的分p无法正常显示
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/799
- Created: 2024-09-04T15:33:49Z
- Owner: user
- Visual subitem: The episode selector overflows when a video has more than 100 parts.
- Observable: The part list does not fit/display normally above 100 episodes.
- Reason: The issue describes a visible overflow in the selector layout.

## #816 — 选集界面错误换行
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/816
- Created: 2024-09-17T01:50:13Z
- Owner: user
- Visual subitem: Text wraps incorrectly in the episode-selection interface.
- Observable: Episode entries break onto the wrong lines.
- Reason: This is a clear text-layout defect.

## #875 — 动态评论区UI bug 和单窗口模式显示bug
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/875
- Created: 2024-10-21T08:56:48Z
- Owner: user
- Visual subitem: The dynamic-comment panel has a transparent background that causes text to overlap.
- Observable: Text from the dynamic view overlaps through the comment panel background.
- Reason: The body directly identifies a transparency/overlap presentation defect.

## #888 — 个人空间页动态评论背景透明问题
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/888
- Created: 2024-10-26T04:13:58Z
- Owner: user
- Visual subitem: The dynamic-comment background in a profile page renders transparent.
- Observable: The comment panel does not cover the content behind it.
- Reason: This is a clear background-rendering defect.

## #897 — win11 24h2 全屏播放左上箭头不消失
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/897
- Created: 2024-10-28T17:02:34Z
- Owner: user
- Visual subitem: A back arrow remains visible in the upper-left during full-screen playback.
- Observable: The upper-left back button cannot be hidden in full screen.
- Reason: The report is a visible unwanted overlay/control-placement issue.

## #900 — 状态栏卡片右上角与功能键重叠
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/900
- Created: 2024-10-30T13:02:06Z
- Owner: user
- Visual subitem: Status cards overlap the window’s minimize/maximize/close buttons.
- Observable: With enough cards, they cover the window controls and can cause accidental clicks.
- Reason: This is a clear layout collision with visible controls.

## #910 — biliuwp4.7.6版本详情页透明化
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/910
- Created: 2024-11-08T07:56:47Z
- Owner: user
- Visual subitem: The video details panel renders transparent.
- Observable: The details text is drawn over underlying content and is difficult to read.
- Reason: This is a clear rendering/background defect.

## #922 — 部分控件颜色与设置主题不匹配
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/922
- Created: 2024-11-22T13:43:25Z
- Owner: user
- Visual subitem: Some controls/text use colors that do not match the selected light theme.
- Observable: Some labels are difficult to distinguish in light mode.
- Reason: This is a direct theme contrast/color defect.

## #943 — 全屏化后有部分视频弹幕被遮挡
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/943
- Created: 2024-12-16T11:36:12Z
- Owner: user
- Visual subitem: Some video danmaku are hidden/clipped at the top after entering full screen.
- Observable: The top part of some danmaku is obscured in full screen.
- Reason: This is a clear overlay clipping/positioning defect.

## #953 — 主题设置为跟随系统时颜色错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/953
- Created: 2024-12-21T09:29:24Z
- Owner: user
- Visual subitem: Following the system theme selects incorrect colors.
- Observable: Controls use the wrong colors when theme follows Windows.
- Reason: This is a clear theme-color presentation defect.

## #981 — 直播间弹幕输入框×键与表情键重叠，概率触发自动删除
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/981
- Created: 2025-01-10T10:52:06Z
- Owner: user
- Visual subitem: The close and emoji buttons in live-chat input overlap.
- Observable: The × and emoji controls occupy overlapping space beside the input.
- Reason: The overlap is a clear visual-layout subitem; the separate deletion of the last typed character remains pending logic review.
- Pending logic subitems: Typing can delete the last character in live chat.

## #1044 — 三大金刚键重复显示错误的颜色
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1044
- Created: 2025-02-10T07:49:45Z
- Owner: user
- Visual subitem: The three window-control buttons repeat or display the wrong theme color.
- Observable: The minimize/maximize/close buttons use duplicate or incorrect colors.
- Reason: This is a clear control-color appearance defect.

## #1047 — 自定义倍速界面和快捷键设置界面在Win10中宽度丢失
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1047
- Created: 2025-02-10T13:42:31Z
- Owner: user
- Visual subitem: The custom-speed and shortcut-settings dialogs lose their width on Windows 10.
- Observable: Those settings screens appear too narrow on Windows 10.
- Reason: This is a clear dialog-layout defect.

## #1066 — [Bug] 更改主题颜色为“跟随系统”后登陆界面文字不显示
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1066
- Created: 2025-02-19T03:35:39Z
- Owner: user
- Visual subitem: Login-method labels disappear when the theme is set to follow the system.
- Observable: The labels for SMS, QR, and password login are invisible.
- Reason: This is a clear text-contrast/rendering defect; login behavior itself is not classified here.

## #1068 — 直播间粉丝牌id以及等级丢失颜色
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1068
- Created: 2025-02-19T05:05:34Z
- Owner: user
- Visual subitem: Live fan-badge IDs and levels lose their color and turn white.
- Observable: Badge text appears white instead of using its prior colors.
- Reason: This is a clear text-color presentation regression.

## #1093 — 评论区用户名后方偶尔会出现奇怪的白色方块
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1093
- Created: 2025-03-05T03:58:52Z
- Owner: user
- Visual subitem: Some comment usernames have unexplained white boxes rendered beside them.
- Observable: White square artifacts appear beside some usernames.
- Reason: This is a clear visible rendering artifact in the comment list.

## #1094 — 评论区文本及元素没有对齐
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1094
- Created: 2025-03-05T04:10:17Z
- Owner: user
- Visual subitem: Comment text and UI elements are misaligned in both collapsed and expanded comments.
- Observable: The comment rows do not align to the expected text/input edges.
- Reason: This is a direct alignment/layout defect.

## #1139 — 直播间高能用户榜单文字重叠
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1139
- Created: 2025-04-16T11:14:27Z
- Owner: user
- Visual subitem: Text and numbers overlap in the live high-energy-user ranking.
- Observable: Names and numeric values run together in the ranking.
- Reason: This is a clear typography/layout collision.

## #1210 — 评论区点赞数显示不合理
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1210
- Created: 2025-06-06T07:33:47Z
- Owner: user
- Visual subitem: Large like counts use an awkward display format (“57.0千” instead of “5.7万”).
- Observable: A large comment like count is formatted in an unexpected locale/unit style.
- Reason: The underlying count is equivalent; the defect is its visible number formatting.

## #1240 — 某些专栏正文内容显示异常
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1240
- Created: 2025-06-22T14:37:16Z
- Owner: user
- Visual subitem: Article text overlaps the leading image in some columns.
- Observable: Text below the first image is drawn on top of that image.
- Reason: This is a direct content-layout overlap defect.

## #1251 — 视频章节字体颜色问题
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1251
- Created: 2025-06-29T09:17:48Z
- Owner: user
- Visual subitem: In light mode, chapter text is almost unreadable against its dark translucent background.
- Observable: Chapter labels blend into the dark overlay in light mode.
- Reason: This is a clear theme contrast/color defect.

## #1335 — 文章阅读器样式错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1335
- Created: 2025-10-18T08:31:37Z
- Owner: user
- Visual subitem: The article author avatar elements do not overlap as intended, leaving the avatar split across lines.
- Observable: The author avatar/header elements wrap onto separate lines.
- Reason: This is a clear alignment/layout defect in the article header.

## #1410 — 高级弹幕无法正确显示/显示错误
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1410
- Created: 2026-03-02T15:47:08Z
- Owner: user
- Visual subitem: Advanced danmaku render incorrectly in the player.
- Observable: Advanced danmaku appear in the wrong form/position.
- Reason: The report is specifically about the visible rendering of advanced danmaku; no data/API or playback failure is described.

## #1423 — 热门榜单大会员标签遮挡
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1423
- Created: 2026-03-21T11:35:37Z
- Owner: user
- Visual subitem: A membership badge overlaps the title in the popular ranking list.
- Observable: The badge covers enough of the title that the item name cannot be read.
- Reason: This is a direct element-overlap defect.

## #1431 — 弹幕过长时弹幕无法显示完全
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1431
- Created: 2026-04-11T15:13:11Z
- Owner: user
- Visual subitem: Long danmaku are clipped at the end.
- Observable: The final characters of long danmaku are missing from view.
- Reason: This is a clear text-clipping defect in the overlay.

## #1454 — 修复评论多图显示效果
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1454
- Created: 2026-05-27T11:18:58Z
- Owner: user
- Visual subitem: The layout/display of multiple images in comments is incorrect.
- Observable: Comment images are arranged or rendered incorrectly.
- Reason: The title identifies a correction to existing multi-image presentation, a clear display/layout issue.

## #1469 — 4.8.16.1801版本无法循环播放
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1469
- Created: 2026-08-07T11:17:54Z
- Owner: user
- Visual subitem: Two video-tab interfaces can overlap when opening another video.
- Observable: The second video tab is drawn on top of the first in the reported case.
- Reason: This is a visible page-overlap subitem; the separate loop-playback black-screen/hang remains pending logic review.
- Pending logic subitems: Loop playback can end in a black loading screen and leave the app unresponsive.

## #1498 — 预发布版本视频详情页关闭时出现残留
- URL: https://github.com/ywmoyue/biliuwp-lite/issues/1498
- Created: 2026-10-16T15:33:20Z
- Owner: user
- Visual subitem: Player UI elements remain visible after the finished video tab is closed or switched away.
- Observable: Elements from the closed video page remain on screen.
- Reason: The issue explicitly reports residual interface elements after page close, a clear visual lifecycle artifact.
