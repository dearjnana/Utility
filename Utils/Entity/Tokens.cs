﻿using Insight.Utils.Common;

namespace Insight.Utils.Entity
{
    public class TokenPackage
    {
        /// <summary>
        /// AccessToken字符串
        /// </summary>
        public string accessToken { get; set; }

        /// <summary>
        /// RefresToken字符串
        /// </summary>
        public string refreshToken { get; set; }

        /// <summary>
        /// Secret过期时间
        /// </summary>
        public string userId => Util.deserialize<AccessToken>(Util.base64Decode(accessToken)).userId;
    }

    public class AccessToken
    {
        /// <summary>
        /// 令牌ID
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string userId { get; set; }
    }

    public class UserInfo
    {
        /// <summary>
        /// ID，唯一标识
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string tenantId { get; set; }

        /// <summary>
        /// 用户编码
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 姓名/昵称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 登录账号
        /// </summary>
        public string account { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string mobile { get; set; }

        /// <summary>
        /// 注册邮箱
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// 用户头像URL
        /// </summary>
        public string headImg { get; set; }

        /// <summary>
        /// 是否内置
        /// </summary>
        public string builtin { get; set; }

        /// <summary>
        /// 用户注册时间
        /// </summary>
        public string createdTime { get; set; }
    }
}
