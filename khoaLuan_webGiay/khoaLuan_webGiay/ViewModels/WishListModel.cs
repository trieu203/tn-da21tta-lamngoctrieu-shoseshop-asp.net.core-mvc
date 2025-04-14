namespace khoaLuan_webGiay.ViewModels
{
    public class WishListModel
    {
        public int Quantity { get; set; }
    }

    public class WishListViewModel
    {
        public int WishListId { get; set; }

        public int? UserId { get; set; }

        public int? ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime? AddedDate { get; set; }
    }
}
