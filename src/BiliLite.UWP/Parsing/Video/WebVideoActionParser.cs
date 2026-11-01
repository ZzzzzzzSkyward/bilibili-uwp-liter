using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Video
{
    /// <summary>
    /// 详情页的动作端点（点赞、取消赞、三连、投币、收藏、关注）的解析：只回答"成了没有，服务端说了什么"。
    /// 不弹提示（发布者据此弹 toast）、不改任何状态、不记日志。
    ///
    /// 字段/行为约定（媒体：详情页真实响应形状；夹具见 plan/tests/c1）
    ///
    /// | 线上路径 | 线上类型           | 归一化                     | 缺失/null 时             |
    /// | -------- | ------------------ | -------------------------- | ------------------------ |
    /// | code     | int                | 业务码，非 0 = Rejected    | 缺失/非数字 = 信封无效   |
    /// | data     | object             | 只取 data.toast 作为提示语 | 没有话，成功由 code 决定 |
    /// | data     | null / 其它类型    | 没有话                     | 同上                     |
    /// | data.toast | string           | 原样                       | 空串 = 没有话            |
    ///
    /// 1. 成功与否只看 code：改造前也是 `data.success`（即 code==0），
    ///    但那时用 `data.data["toast"]` 读提示语，data 为 null 会 NRE 再被 catch 成"操作失败"。
    ///    这里把"没有 data"当成"没有话"，动作仍然是成功的。
    /// 2. data 可能是 null，所以走 VideoEnvelope 取原始 JToken，而不是只接受对象的 ApiEnvelope.Read。
    /// 3. 返回成功值是一句话：空串表示"服务端没说"，用哪句兜底由发布者定。
    /// </summary>
    public static class WebVideoActionParser
    {
        public static ApiParseResult<string> ParseWebVideoAction(HttpResults response)
        {
            var envelope = VideoEnvelope.ReadDataToken(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<string>.FromFailure(envelope);
            }

            var data = envelope.Value;
            if (data == null || data.Type != JTokenType.Object)
            {
                return ApiParseResult<string>.Success("");
            }
            var toast = data["toast"];
            if (toast == null || toast.Type == JTokenType.Null)
            {
                return ApiParseResult<string>.Success("");
            }
            return ApiParseResult<string>.Success(toast.ToString());
        }
    }
}
