#region Usings

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Enums;

#endregion

namespace UltimatePlaylist.Common.Mvc.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        #region Authorize

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var tokenString = httpContext.Request.Query["token"].ToString().Replace("Bearer ", string.Empty);

            if (!string.IsNullOrEmpty(tokenString))
            {
                if (CheckToken(tokenString, out var expires))
                {
                    httpContext.Response.Cookies.Append("hangfireauth", tokenString, new CookieOptions() { });

                    return true;
                }
            }
            else
            {
                var cookies = httpContext.Request.Cookies;

                if (!string.IsNullOrEmpty(cookies["hangfireauth"]))
                {
                    return CheckToken(cookies["hangfireauth"]);
                }
            }

            return false;
            //return true;
        }

        #endregion

        #region Private methods

        private bool CheckToken(string tokenString)
        {
            return CheckToken(tokenString, out _);
        }

        private bool CheckToken(string tokenString, out DateTime expires)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.ReadJwtToken(tokenString);

            expires = token.ValidTo;

            return token.Claims.Any(c => c.Type == JwtClaims.Role && c.Value == UserRole.Administrator.ToString());
        }

        #endregion
    }
}