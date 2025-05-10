using System.ComponentModel.DataAnnotations;

namespace khoaLuan_webGiay.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email phải thuộc miền @gmail.com.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
        public string? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DiscountCode { get; set; }
        public int DiscountAmount { get; set; }
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal Subtotal => CartItems.Sum(x => x.PriceAtTime * x.Quantity);
        public decimal TotalDiscount => CartItems.Sum(x =>
            x.PriceAtTime * x.Quantity * ((decimal)x.Discount / 100));
        public int ShippingFee => 50000;
        public decimal TotalAmount => Subtotal - TotalDiscount + ShippingFee;

    }
}
