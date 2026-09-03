using DeepCore;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace DeepFrozen.ASP
{
    [AttributeUsage(AttributeTargets.Method)]
    public class WebRequestAttribute : System.Attribute
    {
        public string Pattern { get; private set; }
        public string Method { get; private set; }
        public WebRequestAttribute(string method, string pattern)
        {
            this.Pattern = pattern;
            this.Method = method;
        }
    }

    public abstract class WebAPI : Disposable
    {
        protected readonly WebApplication app;
        public WebApplication App => app;
        public WebAPI(WebApplication app)
        {
            this.app = app;
            foreach (var item in PropertyUtil.GetMembersWithAttribute<MethodInfo, WebRequestAttribute>(this.GetType()))
            {
                var method = item.Item1;
                var attr = item.Item2;
                var call =  Delegate.CreateDelegate(typeof(RequestDelegate), this, method);
                switch (attr.Method.ToUpper())
                {
                    case "GET":
                        app.MapGet(attr.Pattern, new RequestDelegate(async ctx => {

                            await method.Invoke(this, new bo);
                        }));
                        break;
                    case "POST":
                        app.MapPost(attr.Pattern, Delegate.CreateDelegate(typeof(RequestDelegate), this, method));
                        break;
                }
            }
            //----------------------------------------------------------------
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                this.Dispose();
            });
        }


    }

}
