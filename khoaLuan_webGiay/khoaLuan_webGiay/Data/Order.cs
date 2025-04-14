using System;
using System.Collections.Generic;

namespace khoaLuan_webGiay.Data;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? OrderStatus { get; set; }

    public string? ShippingAddress { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsConfirmed { get; set; }

    public virtual ICollection<MailConfirmation> MailConfirmations { get; set; } = new List<MailConfirmation>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? User { get; set; }
}
