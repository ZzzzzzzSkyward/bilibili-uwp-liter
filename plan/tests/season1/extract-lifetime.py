#!/usr/bin/env python3
"""Compile complete actual season/shell methods; reject ambiguous anchors and bind all bytes.
PlayerControl is deliberately not read: the boundary spy counts attempted calls.
"""
import argparse
import hashlib
import json
from pathlib import Path


def block_end(text, opening):
    depth = 0
    state = 'code'
    i = opening
    while i < len(text):
        c = text[i]
        n = text[i:i + 2]
        if state == 'line':
            if c == '\n': state = 'code'
        elif state == 'comment':
            if n == '*/': state = 'code'; i += 1
        elif state in ('string', 'char'):
            if c == '\\': i += 1
            elif c == ('"' if state == 'string' else "'"): state = 'code'
        elif state == 'verbatim':
            if n == '""': i += 1
            elif c == '"': state = 'code'
        elif n == '//': state = 'line'; i += 1
        elif n == '/*': state = 'comment'; i += 1
        elif n == '@"': state = 'verbatim'; i += 1
        elif c == '"': state = 'string'
        elif c == "'": state = 'char'
        elif c == '{': depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0: return i + 1
        i += 1
    raise ValueError('Unterminated C# block')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifest = []

    def source(rel):
        p = args.source_root / rel
        return p, p.read_text(encoding='utf-8-sig')

    def bind(rel, text, start, end, anchor):
        p, _ = source(rel)
        body = text[start:end]
        manifest.append(dict(path=rel, anchor=anchor,
                             source_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),
                             extraction_sha256=hashlib.sha256(body.encode()).hexdigest(),
                             start_line=text[:start].count('\n') + 1,
                             end_line=text[:end].count('\n') + 1))
        return body

    def extract(rel, anchor):
        _, text = source(rel)
        if text.count(anchor) != 1: raise ValueError(f'Ambiguous/missing anchor: {rel}: {anchor}')
        start = text.index(anchor)
        return bind(rel, text, start, block_end(text, text.index('{', start)), anchor)

    page = 'src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs'
    _, text = source(page)
    start_anchor = 'public sealed partial class SeasonDetailPage : PlayPage'
    end_anchor = 'public SeasonDetailPage()'
    if text.count(start_anchor) != 1 or text.count(end_anchor) != 1:
        raise ValueError('Ambiguous/missing page field anchors')
    start = text.index('{', text.index(start_anchor)) + 1
    fields = bind(page, text, start, text.index(end_anchor), 'complete page fields before constructor')
    anchors = ['private void SeasonDetailPage_Loaded(object sender, RoutedEventArgs e)',
               'private void SubscribeClosedPage()', 'private void UnsubscribeClosedPage()',
               'private void SubscribeShare()', 'private void ReleaseShare()',
               'private void SeasonDetailPage_ClosedPage(object sender, EventArgs e)',
               'private void ClosePage()',
               'protected async override void OnNavigatedTo(NavigationEventArgs e)',
               'protected override void OnNavigatedFrom(NavigationEventArgs e)',
               'protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)',
               'private async Task InitSeasonDetail()', 'private void InitializePlayInfo()',
               'private void UpdatePlayInfoToEpisode(int index)',
               'private void UpdatePlayInfoToPreview(int index)', 'public void ChangeTitle(string title)',
               'private async void btnRefresh_Click(object sender, RoutedEventArgs e)']
    methods = '\n'.join(extract(page, anchor) for anchor in anchors)
    shell = extract('src/BiliLite.UWP/MainPage.xaml.cs', 'private void ClosePage(TabViewItem tabItem)')
    frame = extract('src/BiliLite.UWP/Controls/MyFrame.cs', 'public class MyFrame:Frame')
    output = '''// Generated from complete actual product method text. Do not edit.
using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks;
using BiliLite.Modules; using BiliLite.Helpers;
using Api = SeasonLifetimeHarness.PageApi;
using Utils = SeasonLifetimeHarness.PageUtils;
using SettingHelper = SeasonLifetimeHarness.PageSettings;
namespace SeasonLifetimeHarness {
public partial class SeasonPage : PlayPage {
''' + fields + '\n' + methods + '''
}
public partial class Shell {
''' + shell + '\n}\n' + frame + '\n}\n'
    review = extract(page, 'private async void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)')
    output += """
namespace SeasonReviewCallerHarness {
public class SelectionChangedEventArgs : EventArgs {}
public class Pivot { public int SelectedIndex; }
public class Caller {
public Pivot pivot = new Pivot();
public BiliLite.Modules.Season.SeasonReviewVM seasonReviewVM;
public Caller(BiliLite.Modules.Season.SeasonReviewVM vm) { seasonReviewVM = vm; }
public void Enter(int index) { pivot.SelectedIndex=index; pivot_SelectionChanged(null, new SelectionChangedEventArgs()); }
""" + review + '\n}\n}\n'
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(output)
    args.output.with_suffix('.manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    main()
