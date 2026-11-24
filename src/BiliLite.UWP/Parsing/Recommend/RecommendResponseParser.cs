using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Modules;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Recommend
{
    public static class RecommendResponseParser
    {
        public static ApiParseResult<List<RecommendItemModel>> Read(HttpResults response, out bool optionalBannerFailure)
        {
            optionalBannerFailure = false;
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess) return ApiParseResult<List<RecommendItemModel>>.FromFailure(envelope);
            var array = envelope.Value["items"] as JArray;
            if (array == null) return ApiParseResult<List<RecommendItemModel>>.Fail(ApiParseFailure.Payload, "data.items", "推荐列表格式错误");
            try
            {
                var rows = new List<RecommendItemModel>();
                foreach (var token in array)
                {
                    if (token.Type != JTokenType.Object) throw new InvalidOperationException("推荐卡片格式错误");
                    var wire = JObject.Parse(token.ToString().Replace("left_bottom_rcmd_reason_style", "rcmd_reason_style"));
                    try
                    {
                        var row = wire.ToObject<RecommendItemModel>();
                        if (row == null) throw new InvalidOperationException("推荐卡片为空");
                        rows.Add(row);
                    }
                    catch when (wire["card_goto"]?.Type == JTokenType.String && (string)wire["card_goto"] == "banner")
                    { optionalBannerFailure = true; }
                }
                return ApiParseResult<List<RecommendItemModel>>.Success(rows);
            }
            catch (Exception ex) { return ApiParseResult<List<RecommendItemModel>>.Fail(ApiParseFailure.Payload, "data.items", ex.Message); }
        }
        public static bool ActionSucceeded(HttpResults response)
        {
            JObject body; int code;
            return response != null && response.status && response.TryGetJObject(out body) && ApiEnvelope.TryReadBusinessCode(body["code"], out code) && code == 0;
        }
    }
}
