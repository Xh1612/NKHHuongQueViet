using HuongQueViet.Helpers;
using HuongQueViet.Models;

namespace HuongQueViet.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(Order order, HttpContext context);
        (bool IsValid, bool IsSuccess, string TxnRef) ProcessReturn(IQueryCollection query);
    }

    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        public VnPayService(IConfiguration config) { _config = config; }

        public string CreatePaymentUrl(Order order, HttpContext context)
        {
            var vnpay = new VnPayLibrary();
            var txnRef = $"{order.Id}_{DateTime.Now.Ticks}";
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _config["Vnpay:TmnCode"]!);
            vnpay.AddRequestData("vnp_Amount", ((long)(order.TotalAmount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["Vnpay:ReturnUrl"]!);
            vnpay.AddRequestData("vnp_TxnRef", txnRef);
            return vnpay.CreateRequestUrl(_config["Vnpay:BaseUrl"]!, _config["Vnpay:HashSecret"]!);
        }

        public (bool, bool, string) ProcessReturn(IQueryCollection query)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in query) if (key.StartsWith("vnp_")) vnpay.AddResponseData(key, value.ToString());
            var isValid = vnpay.ValidateSignature(query["vnp_SecureHash"].ToString(), _config["Vnpay:HashSecret"]!);
            return (isValid, vnpay.GetResponseData("vnp_ResponseCode") == "00", vnpay.GetResponseData("vnp_TxnRef"));
        }
    }
}