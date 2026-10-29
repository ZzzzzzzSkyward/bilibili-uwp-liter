using BiliLite.Api;

namespace BiliLite.Models.Requests.Api
{
    public class EmoteApi : BaseApi
    {
        public ApiModel UserEmote(BiliLite.Api.EmoteBusiness business)
        {
            var parameter = ApiHelper.MustParameter(AndroidKey, true) + "&business=" + business;
            parameter += ApiHelper.GetSign(parameter, AndroidKey);
            return CreateGet(ApiBaseUrl + "/x/emote/user/panel/web", parameter,
                ApiHelper.GetAuroraHeaders(), needCookie: true);
        }
    }
}
