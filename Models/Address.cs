using System.ComponentModel.DataAnnotations;

namespace HuongQueViet.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại nhận hàng")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public bool IsDefault { get; set; }
    }
}