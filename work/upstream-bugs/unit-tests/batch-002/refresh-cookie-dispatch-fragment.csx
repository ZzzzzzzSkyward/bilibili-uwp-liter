            var url3 = "https://passport.bilibili.com/x/passport-login/web/confirm/refresh";
            var api3 = new ApiModel()
            {
                need_cookie = true,
                method = RestSharp.Method.Post,
                baseUrl = url3,
                body = $"csrf={GetCSRF()}&refresh_token={token}"
            };
            var result3 = await api.Request();
            _need_refresh_cookie = false;
