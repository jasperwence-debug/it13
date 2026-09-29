using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace CRM.winforms.Services
{
    public abstract class ApiClientBase
    {
        protected static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(AppConfig.ApiBaseUrl)
            };

            if (!string.IsNullOrEmpty(SessionManager.Current.Token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", SessionManager.Current.Token);
            }

            return client;
        }

        // ─── GET ──────────────────────────────────────────────
        protected static async Task<T?> GetAsync<T>(string url)
        {
            using var client = CreateClient();
            var resp = await client.GetAsync(url);
            return await DeserializeOrThrow<T>(resp);
        }

        // ─── POST ─────────────────────────────────────────────
        protected static async Task<T?> PostAsync<T>(string url, object body)
        {
            using var client = CreateClient();
            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            return await DeserializeOrThrow<T>(resp);
        }

        // ─── PUT ──────────────────────────────────────────────
        protected static async Task<T?> PutAsync<T>(string url, object body)
        {
            using var client = CreateClient();
            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PutAsync(url, content);
            return await DeserializeOrThrow<T>(resp);
        }

        // ─── PATCH (void — no response body) ──────────────────
        protected static async Task PatchAsync(string url, object? body = null)
        {
            using var client = CreateClient();
            var json = body != null ? JsonConvert.SerializeObject(body) : "";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            var resp = await client.SendAsync(req);
            await EnsureSuccessOrThrow(resp);
        }

        // ─── PATCH (generic — returns response body) ──────────
        protected static async Task<T?> PatchAsync<T>(string url, object? body = null)
        {
            using var client = CreateClient();
            var json = body != null ? JsonConvert.SerializeObject(body) : "";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            var resp = await client.SendAsync(req);
            return await DeserializeOrThrow<T>(resp);
        }

        // ─── DELETE ───────────────────────────────────────────
        // Renamed to DeleteRequestAsync to avoid clashing with
        // subclass methods named DeleteAsync(Guid).
        protected static async Task DeleteRequestAsync(string url)
        {
            using var client = CreateClient();
            var resp = await client.DeleteAsync(url);
            await EnsureSuccessOrThrow(resp);
        }

        // ─── Helpers ──────────────────────────────────────────
        private static async Task<T?> DeserializeOrThrow<T>(HttpResponseMessage resp)
        {
            await EnsureSuccessOrThrow(resp);
            var raw = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw)) return default;
            return JsonConvert.DeserializeObject<T>(raw);
        }

        private static async Task EnsureSuccessOrThrow(HttpResponseMessage resp)
        {
            if (resp.IsSuccessStatusCode) return;

            var raw = await resp.Content.ReadAsStringAsync();
            string message = $"HTTP {(int)resp.StatusCode}";

            try
            {
                dynamic? err = JsonConvert.DeserializeObject(raw);
                if (err?.message != null)
                    message = (string)err.message;
            }
            catch { /* fall through */ }

            throw new ApiException(message, resp.StatusCode);
        }
    }

    public class ApiException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public ApiException(string message, System.Net.HttpStatusCode code)
            : base(message) => StatusCode = code;
    }
}