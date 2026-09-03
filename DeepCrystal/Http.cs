using DeepCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal
{
    public class Http
    {
        public readonly Uri root;
        public Http(string root) { this.root = new Uri(root); }
        public Http(Uri root) { this.root = root; }

        public Task<string> get(string url) { return Get(new Uri(root, url)); }
        public Task<string> get(Uri url) { return Get(new Uri(root, url)); }

        public Task<string> post(string url, string str) { return Post(new Uri(root , url), str); }
        public Task<string> post(Uri url, string str) { return Post(new Uri(root , url), str); }


        public Task<string> postJsonBase64(string url, dynamic any)
        {
            return post(url, CUtils.ToBase64(JSON.Serialize(any)));
        }
        public Task<string> postJsonBase64(Uri url, dynamic any)
        {
            return post(url, CUtils.ToBase64(JSON.Serialize(any)));
        }
        public async Task<T> postJsonBase64<T>(string url, dynamic any)
        {
            var json = await post(url, CUtils.ToBase64(JSON.Serialize(any)));
            return JSON.Deserialize<T>(json);
        }
        public async Task<T> postJsonBase64<T>(Uri url, dynamic any)
        {
            var json = await post(url, CUtils.ToBase64(JSON.Serialize(any)));
            return JSON.Deserialize<T>(json);
        }


        public static async Task<string> Get(string url)
        {
            return await Get(new Uri(url));
        }
        public static async Task<string> Get(Uri url)
        {
            //Console.WriteLine("get " + url);
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseBody);
                return responseBody;
            }
        }
        public static async Task<string> Post(string url, string str)
        {
            return await Post(new Uri(url), str);
        }
        public static async Task<string> Post(Uri url, string str)
        {
            //Console.WriteLine("post " + url);
            var content = new StringContent(str);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseBody);
                return responseBody;
            }
        }
        public static async Task<string> PostJsonBase64(Uri url, dynamic any)
        {
            return await Post(url, CUtils.ToBase64(JSON.Serialize(any)));
        }
        public static async Task<string> PostJsonBase64(string url, dynamic any)
        {
            return await Post(url, CUtils.ToBase64(JSON.Serialize(any)));
        }
        public static async Task<T> PostJsonBase64<T>(Uri url, dynamic any)
        {
            var json = await Post(url, CUtils.ToBase64(JSON.Serialize(any)));
            return JSON.Deserialize<T>(json);
        }
        public static async Task<T> PostJsonBase64<T>(string url, dynamic any)
        {
            var json = await Post(url, CUtils.ToBase64(JSON.Serialize(any)));
            return JSON.Deserialize<T>(json);
        }

    }
}
