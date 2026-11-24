#!/usr/bin/env python3
"""Whole linked product files plus unique exact slices; output lives only in ignored obj."""
from pathlib import Path
import hashlib,json,re
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent/'obj';out.mkdir(exist_ok=True)
manifest={};bodies=[]
def read(path):
    p=root/'src/BiliLite.UWP'/path;manifest[str(p.relative_to(root))]=hashlib.sha256(p.read_bytes()).hexdigest();return p.read_text(encoding='utf-8-sig')
def block(source,marker):
    if source.count(marker)!=1: raise SystemExit('nonunique marker: '+marker)
    start=source.index(marker);brace=source.index('{',start);depth=0
    # Tokenize comments/strings separately so interpolation braces do not affect source boundaries.
    token=re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/|[{}]',re.S)
    for match in token.finditer(source,brace):
        if match.group()=='{':depth+=1
        elif match.group()=='}':
            depth-=1
            if depth==0:
                body=source[start:match.end()];assert source.count(body)==1;bodies.append({'marker':marker,'sha256':hashlib.sha256(body.encode()).hexdigest()});return body
    raise SystemExit('unclosed '+marker)
request=block(read('Api/ApiHelper.cs'),'public class ApiModel')
http=block(read('Helpers/HttpHelper.cs'),'public class HttpResults')
scalar=block(read('Helpers/Utils.cs'),'public static int ToInt32(this object obj)')
page=read('Pages/DownloadPage.xaml.cs');handlers='\n'.join(block(page,m) for m in ['private async void btnEpisodesDelete_Click','private async void btnMenuDetele_Click'])
top=block(read('Pages/Home/RecommendPage.xaml.cs'),'private void btnTop_Click')
season=read('Pages/SeasonDetailPage.xaml.cs');condition=block(season,'if (DownloadVM.Instance.Downloadings.FirstOrDefault')
login_source=read('Helpers/Utils.cs')
login=block(login_source,'public async static Task<bool> ShowLoginDialog()')
field='private static bool dialogShowing = false;'
assert login_source.count(field)==1
bodies.append({'marker':field,'sha256':hashlib.sha256(field.encode()).hexdigest()})
play=read('Modules/Player/Playurl/BiliPayUrlRequest.cs')
codec=block(play,'enum PlayUrlCodecMode')
parse=block(play,'protected async Task<BiliPlayUrlQualitesInfo> ParseJson(')
handle=block(play,'private async Task<string> HandleUrl(')
dtos='\n'.join(block(play,m) for m in ['public class DashItemModel','public class SegmentBase','public class FlvDurlModel'])
forward=block(read('Modules/Player/PlayerVM.cs'),'public async Task<BiliPlayUrlQualitesInfo> GetPlayUrls(')
source='''using System.Collections.Generic; using System; using System.Threading.Tasks; using Newtonsoft.Json; using Newtonsoft.Json.Linq; using BiliLite.Helpers; using BiliLite.Models; using BiliLite.Modules; using Windows.Storage; using Windows.UI.Xaml; using Windows.UI.Xaml.Controls; using BiliLite.Dialogs; using BiliLite.Controls; using BiliLite.Modules.Player.Playurl;
namespace BiliLite.Helpers {'''+http+' public static partial class Utils {'+scalar+field+login+'''}}
namespace BiliLite.Api {'''+request+'''}
namespace BiliLite.Pages {public class DownloadPage {
private bool completedCleanupPending;
public DownloadVM downloadVM; public ContentControl Pane = new ContentControl();
public void DeleteEpisode(DownloadedItem parent,DownloadedSubItem item){Pane.DataContext=parent;btnEpisodesDelete_Click(new AppBarButton{DataContext=item},new RoutedEventArgs());}
public void DeleteTitle(DownloadedItem item){btnMenuDetele_Click(new MenuFlyoutItem{DataContext=item},new RoutedEventArgs());}
'''+handlers+'''}
public class RecommendPage {public GridView RecommendGridView=new GridView();public void Top(){btnTop_Click(null,new RoutedEventArgs());}'''+top+'''}}
public class SeasonItem {public long id;public string cid; public int State;}
public static class SeasonCaller {public static int CurrentState(SeasonItem item){int state=0;'''+condition+''';return state;}}
'''

source += 'namespace BiliLite.Modules.Player.Playurl {'+codec+"""
class BiliPlayUrlRequest {
private int ReplaceCDNMode=3; private bool IsVIP=true; private PlayUrlCodecMode CodecMode=PlayUrlCodecMode.DASH_H264;
private Task<string> ReplaceCDN(string url,string userAgent,string referer){throw new Exception("unexpected native CDN path");}
public Task<BiliPlayUrlQualitesInfo> Map(JObject body){return ParseJson(80,body,"agent","https://www.bilibili.com",false,new PlayInfo{avid="av1",cid="cid1"});}
"""+parse+handle+'}'+dtos+'}'
source += 'namespace BiliLite.Modules { public class PlayerVM { readonly DeliveryBridge biliPlayUrl=new DeliveryBridge(); public PlayerVM(bool isDownload){} public Task<PlayerInfo> GetPlayInfo(string avid,string cid)=>DeliveryBridge.GetInfo(avid,cid);'+forward+'}}'
# Fixed source slice contracts: changing a real source seam requires explicit review.
expected={'enum PlayUrlCodecMode': '2dd17cdf18ac6a95269f16a715926312f89b7d892914e2012bad20f50a8257cf', 'protected async Task<BiliPlayUrlQualitesInfo> ParseJson(': '5f035b86f92de7e69615a2bdea536b36652369236c2ec18d1cfd841f1563bdc9', 'private async Task<string> HandleUrl(': '5f9ad11851ba124abf265d9901490902635b5d5018b90175bdac982072e2f273', 'public class DashItemModel': '52e954e27a00569fd48245ee1ec783045ae4fa57eba94abf953373ece9e91170', 'public class SegmentBase': '2492309eaaf0da529e6d5859c0c7851016f23380cb9cccf941ec66c567285616', 'public class FlvDurlModel': '1bbe8b93aa7296508490bf8d14904140e765f96637fe65606afa0a598b581d8f', 'public async Task<BiliPlayUrlQualitesInfo> GetPlayUrls(': '1a82d694c7f25730f79d4f3de496ede439b799b6fd0301c196d275cda0eacc05', 'public async static Task<bool> ShowLoginDialog()': ('8e682965e4aebf1e2cb5781a688fa771f2d5388d67ff4dd790398f2aae6a1a68', '760b21cc33e11fe2471ab787e383f701e81bb68de6f211256145937664c7ddb1')}
for item in bodies:
    if item['marker'] == 'public async static Task<bool> ShowLoginDialog()': assert item['sha256'] in expected[item['marker']],item
    elif item['marker'] in expected: assert item['sha256']==expected[item['marker']],item

(out/'RecoverySource.cs').write_text(source)
# Record exact hashes of linked full source inputs too.
project=(Path(__file__).parent/'RecoveryProbe.csproj').read_text()
for path in re.findall(r'Compile Include="../../../src/BiliLite.UWP/([^"]+)"',project):read(path)
(out/'source-sha256.json').write_text(json.dumps({'files':manifest,'exact_slices':bodies},indent=2)+'\n')
print('RECOVERY_SOURCE_BINDING_PASS',len(manifest),'files;',len(bodies),'unique exact source slices')
