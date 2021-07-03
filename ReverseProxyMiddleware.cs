using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using RouteData = Microsoft.AspNetCore.Routing.RouteData;
using ReverseProxyAPI.Settings;

namespace ReverseProxyAPI
{
    public class ReverseProxyMiddleware
    {
        /// <summary>
        /// Defines the _httpClient.
        /// </summary>
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Defines the _nextMiddleware.
        /// </summary>
        private readonly RequestDelegate _nextMiddleware;

        /// <summary>
        /// Defines the _tokenSettings.
        /// </summary>
        private readonly TokenSetting _tokenSettings;

        /// <summary>
        /// Defines the isToken.
        /// </summary>
        internal bool isToken = false;

        /// <summary>
        /// Defines the token.
        /// </summary>
        internal string token;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseProxyMiddleware"/> class.
        /// </summary>
        /// <param name="nextMiddleware">The nextMiddleware<see cref="RequestDelegate"/>.</param>
        /// <param name="tokenSettings">The tokenSettings<see cref="IOptions{TokenSetting}"/>.</param>
        public ReverseProxyMiddleware(RequestDelegate nextMiddleware, IOptions<TokenSetting> tokenSettings)
        {
            _nextMiddleware = nextMiddleware;
            _tokenSettings = tokenSettings.Value;
        }

        /// <summary>
        /// The Invoke.
        /// </summary>
        /// <param name="context">The context<see cref="HttpContext"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task Invoke(HttpContext context)
        {
            var targetUri = BuildTargetUri(context.Request).ToString().ToLower();

            var path = context.Request.Path;

            token = GetToken();

            if (path.StartsWithSegments("/favicon.ico") == false && path.StartsWithSegments("/") == false)

            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        // Assuming the API is in the same web application. 


                        client.BaseAddress = new Uri(targetUri);
                        client.DefaultRequestHeaders.Add("x-api-key", _tokenSettings.x_api_key_GetWay);
                        client.DefaultRequestHeaders.Add("Authorization-gw", "Bearer " + token);
                        var response = await client.GetAsync(targetUri);

                        var resData = await response.Content.ReadAsStringAsync();
                        var resObj = JsonConvert.DeserializeObject<object>(resData);

                        var result = new ObjectResult(resObj);

                        RouteData routeData = context.GetRouteData();
                        ActionDescriptor actionDescriptor = new ActionDescriptor();
                        ActionContext actionContext = new ActionContext(context, routeData, actionDescriptor);
                        await result.ExecuteResultAsync(actionContext);
                        // await context.Response.WriteAsync(JsonConvert.SerializeObject(responseString));

                        //byte[] bytes = Encoding.ASCII.GetBytes(responseString);


                        //await context.Response.Body.WriteAsync(bytes);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
                return;
            }

            await _nextMiddleware(context);
        }




        /// <summary>
        /// The BuildTargetUri.
        /// </summary>
        /// <param name="request">The request<see cref="HttpRequest"/>.</param>
        /// <returns>The <see cref="Uri"/>.</returns>
        private Uri BuildTargetUri(HttpRequest request)
        {
            Uri targetUri = null;


            targetUri = new Uri(_tokenSettings.BaseUrl + request.Path);
            if (request.QueryString.HasValue != false)
            {
                string newURl = targetUri.ToString();
                targetUri = new Uri(newURl + request.QueryString);
            }
            return targetUri;
        }


        private string GetToken()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(_tokenSettings.TokenUrl);


                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                string Username = _tokenSettings.Username;
                string Password = _tokenSettings.Password;
                request.Headers.Add("x-api-key", _tokenSettings.x_api_key_Token);
                var basicData = Convert.ToBase64String(
                                              System.Text.ASCIIEncoding.ASCII.GetBytes(
                                                         $"{Username}:{Password}"));
                request.Headers.Add("Authorization", "Basic " + basicData);


                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);

                var token = tokenResponse.access_token;

                return token;
                // isToken = true;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

    }
}
