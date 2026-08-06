using System.Security.Cryptography;
using System.Text;

namespace HuongQueViet.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(StringComparer.Ordinal);
        private readonly SortedList<string, string> _responseData = new(StringComparer.Ordinal);

        public void AddRequestData(string key, string value) { if (!string.IsNullOrEmpty(value)) _requestData[key] = value; }
        public void AddResponseData(string key, string value) { if (!string.IsNullOrEmpty(value)) _responseData[key] = value; }
        public string GetResponseData(string key) => _responseData.TryGetValue(key, out var v) ? v : string.Empty;

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();
            foreach (var (key, value) in _requestData) data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            var signData = data.ToString().TrimEnd('&');
            return baseUrl + "?" + data + "vnp_SecureHash=" + HmacSha512(hashSecret, signData);
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var data = new StringBuilder();
            foreach (var (key, value) in _responseData)
                if (key != "vnp_SecureHash" && key != "vnp_SecureHashType") data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            return HmacSha512(secretKey, data.ToString().TrimEnd('&')).Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string HmacSha512(string key, string inputData)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return string.Concat(hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData)).Select(b => b.ToString("x2")));
        }
    }
}