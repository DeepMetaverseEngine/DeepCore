using DeepCore;
using DeepCore.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using DeepCrystal;

namespace Microsoft.AspNetCore.Http
{

    public delegate Task<T> ProcessWithJsonAsync<T>(dynamic request);

    public delegate Task<string> ProcessWithPropertiesAsync(Properties request);

    public static class WebHelper
    {
        public static async Task<string> ReadAsTextAsync(this HttpRequest req)
        {
            var len = req.ContentLength;
            if (len.HasValue && len > 0)
            {
                var bytes = await IOUtil.ReadExpectAsync(req.Body, (int)len);
                return CUtils.DecodeUTF8(bytes);
            }
            return String.Empty;
        }
        public static async Task<dynamic> ReadAsBase64JsonAsync(this HttpRequest req)
        {
            var base64 = await req.ReadAsTextAsync();
            var jsonText = CUtils.FromBase64(base64);
            dynamic acc = JSON.Deserialize(jsonText);
            return acc;
        }
        public static async Task<Properties> ReadAsPropertiesAsync(this HttpRequest req)
        {
            var base64 = await req.ReadAsTextAsync();
            var jsonText = CUtils.FromBase64(base64);
            var acc = Properties.ParseText(jsonText);
            return acc;
        }

        public static async Task ProcessWithBase64JsonObject<T>(this HttpContext ctx, ProcessWithJsonAsync<T> action)
        {
            try
            {
                dynamic acc = await ctx.Request.ReadAsBase64JsonAsync();
                var result = await action(acc);
                string json = JSON.Serialize(result);
                await ctx.Response.WriteAsync(json);
            }
            catch (Exception ex)
            {
                await ctx.Response.WriteAsync(ex.ToFullMessage());
            }
            finally
            {
                //await ctx.Response.CompleteAsync();
            }
        }
        public static async Task ProcessWithBase64JsonObject(this HttpContext ctx, ProcessWithJsonAsync<string> action)
        {
            try
            {
                dynamic acc = await ctx.Request.ReadAsBase64JsonAsync();
                string result = await action(acc);
                await ctx.Response.WriteAsync(result);
            }
            catch (Exception ex)
            {
                await ctx.Response.WriteAsync(ex.ToFullMessage());
            }
            finally
            {
                //await ctx.Response.CompleteAsync();
            }
        }

        public static async Task ProcessWithProperties<T>(this HttpContext ctx, ProcessWithPropertiesAsync action)
        {
            try
            {
                var prop = await ctx.Request.ReadAsPropertiesAsync();
                var result = await action(prop);
                await ctx.Response.WriteAsync(result);
            }
            catch (Exception ex)
            {
                await ctx.Response.WriteAsync(ex.ToFullMessage()); 
            }
            finally
            {
                //await ctx.Response.CompleteAsync();
            }
        }


    }

}
