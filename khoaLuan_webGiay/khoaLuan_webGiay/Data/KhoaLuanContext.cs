using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace khoaLuan_webGiay.Data;

public partial class KhoaLuanContext : DbContext
{
    public KhoaLuanContext()
    {
    }

    public KhoaLuanContext(DbContextOptions<KhoaLuanContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ChatHistory> ChatHistories { get; set; }

    public virtual DbSet<EmailConfirmation> EmailConfirmations { get; set; }

    public virtual DbSet<MailConfirmation> MailConfirmations { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductSize> ProductSizes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<OtpConfirmations> OtpConfirmations { get; set; }


    public virtual DbSet<WishList> WishLists { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-6JRBMVT\\SQLEXPRESS;Initial Catalog=Khoa_Luan;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__D6AB58B9B3BA1B88");

            entity.Property(e => e.CartId).HasColumnName("Cart_ID");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Carts__User_ID__5629CD9C");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__7B6515217EA24D36");

            entity.Property(e => e.CartItemId).HasColumnName("CartItem_ID");
            entity.Property(e => e.CartId).HasColumnName("Cart_ID");
            entity.Property(e => e.PriceAtTime)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Price_AtTime");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ProductName).HasMaxLength(64);
            entity.Property(e => e.Size)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__CartItems__Cart___59063A47");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__CartItems__Produ__59FA5E80");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB38D6E8074958E");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("Category_Id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("Category_Name");
            entity.Property(e => e.ImageUrl).HasColumnName("Image_Url");
        });

        modelBuilder.Entity<ChatHistory>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__ChatHist__9783B1FEC5FBDDA3");

            entity.ToTable("ChatHistory");

            entity.Property(e => e.ChatId).HasColumnName("Chat_ID");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.ChatHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChatHisto__User___5DCAEF64");
        });

        modelBuilder.Entity<EmailConfirmation>(entity =>
        {
            entity.HasKey(e => e.ConfirmationId).HasName("PK__EmailCon__C6207356974A1B03");

            entity.Property(e => e.ConfirmationId).HasColumnName("Confirmation_ID");
            entity.Property(e => e.ConfirmationCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Confirmation_Code");
            entity.Property(e => e.ExpirationDate)
                .HasColumnType("datetime")
                .HasColumnName("Expiration_Date");
            entity.Property(e => e.IsUsed).HasColumnName("Is_Used");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.EmailConfirmations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__EmailConf__User___3A81B327");
        });

        modelBuilder.Entity<MailConfirmation>(entity =>
        {
            entity.HasKey(e => e.MailId).HasName("PK__MailConf__8C1804FE1A3D061A");

            entity.Property(e => e.MailId).HasColumnName("Mail_ID");
            entity.Property(e => e.EmailSent).HasColumnName("Email_Sent");
            entity.Property(e => e.OrderId).HasColumnName("Order_ID");
            entity.Property(e => e.SentDate)
                .HasColumnType("datetime")
                .HasColumnName("Sent_Date");

            entity.HasOne(d => d.Order).WithMany(p => p.MailConfirmations)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__MailConfi__Order__534D60F1");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__F1E4639B0FF16C20");

            entity.Property(e => e.OrderId).HasColumnName("Order_ID");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsConfirmed).HasColumnName("Is_Confirmed");
            entity.Property(e => e.OrderDate)
                .HasColumnType("datetime")
                .HasColumnName("Order_Date");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasColumnName("Order_Status");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Phone_Number");
            entity.Property(e => e.ShippingAddress)
                .HasMaxLength(255)
                .HasColumnName("Shipping_Address");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_Amount");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Orders__User_ID__4CA06362");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__2F3022E20CD28081");

            entity.Property(e => e.OrderItemId).HasColumnName("OrderItem_ID");
            entity.Property(e => e.OrderId).HasColumnName("Order_ID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.Size)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__OrderItem__Order__4F7CD00D");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__OrderItem__Produ__5070F446");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__9834FB9A274866FD");

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.CategoryId).HasColumnName("Category_Id");
            entity.Property(e => e.CreatedDate).HasColumnName("Created_Date");
            entity.Property(e => e.ImageUrl).HasColumnName("Image_Url");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName)
                .HasMaxLength(64)
                .HasColumnName("Product_Name");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Products__Catego__4222D4EF");
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.HasKey(e => e.ProductSizeId).HasName("PK__ProductS__A38AFB05F045FFA5");

            entity.ToTable("ProductSize");

            entity.Property(e => e.ProductSizeId).HasColumnName("ProductSize_ID");
            entity.Property(e => e.PriceAtTime)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Price_AtTime");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.Size).HasMaxLength(15);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSizes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ProductSi__Produ__44FF419A");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__F85DA7EBEAE18625");

            entity.Property(e => e.ReviewId).HasColumnName("Review_ID");
            entity.Property(e => e.Comment).HasMaxLength(255);
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ReviewDate)
                .HasColumnType("datetime")
                .HasColumnName("Review_Date");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Reviews__Product__47DBAE45");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Reviews__User_ID__48CFD27E");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__206D919096B42AC1");

            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasColumnName("Created_Date");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("Full_Name");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .HasColumnName("User_Name");
        });

        modelBuilder.Entity<WishList>(entity =>
        {
            entity.HasKey(e => e.WishListId).HasName("PK__WishList__57A3DD8FFDC39995");

            entity.ToTable("WishList");

            entity.Property(e => e.WishListId).HasColumnName("WishList_ID");
            entity.Property(e => e.AddedDate)
                .HasColumnType("datetime")
                .HasColumnName("Added_Date");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.WishLists)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__WishList__Produc__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.WishLists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__WishList__User_I__60A75C0F");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
