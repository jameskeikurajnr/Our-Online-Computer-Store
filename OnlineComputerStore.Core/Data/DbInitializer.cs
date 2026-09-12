using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Models;
using System.Linq;

namespace OnlineComputerStore.Core.Data
{
    public static class DbInitializer
    {
        public static void Initialize(StoreDbContext context)
        {
            context.Database.Migrate();

            var catalog = new[]
            {
                new Product
                {
                    Name = "Dell XPS 13",
                    Brand = "Dell",
                    ShortSpecs = "Intel i7 • 16GB RAM • 512GB SSD",
                    Description = "A premium, lightweight ultrabook built for work and study, with a stunning edge-to-edge display.",
                    Price = 1899m,
                    ImageUrl = "/images/dell-xps.jpg",
                    Category = "Laptops",
                    IsFeatured = true,
                    StockQuantity = 18
                },
                new Product
                {
                    Name = "HP Spectre x360",
                    Brand = "HP",
                    ShortSpecs = "Intel i7 • 16GB RAM • 1TB SSD",
                    Description = "A convertible 2-in-1 laptop that flexes between laptop, tablet, and tent modes.",
                    Price = 2099m,
                    ImageUrl = "/images/hp-spectre.jpg",
                    Category = "Laptops",
                    IsFeatured = true,
                    StockQuantity = 12
                },
                new Product
                {
                    Name = "MacBook Air 15\"",
                    Brand = "Apple",
                    ShortSpecs = "Apple M3 • 16GB RAM • 512GB SSD",
                    Description = "Remarkably thin and fast, with all-day battery life for study, work, and everything in between.",
                    Price = 2299m,
                    ImageUrl = "/images/macbook-air.jpg",
                    Category = "Laptops",
                    IsFeatured = true,
                    StockQuantity = 20
                },
                new Product
                {
                    Name = "HP Pavilion Desktop",
                    Brand = "HP",
                    ShortSpecs = "Intel i5 • 16GB RAM • 1TB HDD + 256GB SSD",
                    Description = "A reliable desktop tower for everyday computing, schoolwork, and home office use.",
                    Price = 1099m,
                    ImageUrl = "/images/hp-pavilion.jpg",
                    Category = "Desktops",
                    IsFeatured = false,
                    StockQuantity = 15
                },
                new Product
                {
                    Name = "Logitech MX Master Mouse",
                    Brand = "Logitech",
                    ShortSpecs = "Wireless • Ergonomic • Multi-device",
                    Description = "A precision wireless mouse designed for long sessions of comfortable, productive work.",
                    Price = 129m,
                    ImageUrl = "/images/logitech-mouse.jpg",
                    Category = "Accessories",
                    IsFeatured = false,
                    StockQuantity = 40
                },
                new Product
                {
                    Name = "Lenovo ThinkPad X1 Carbon",
                    Brand = "Lenovo",
                    ShortSpecs = "Intel i7 • 16GB RAM • 1TB SSD",
                    Description = "A legendary business laptop — durable, lightweight, and built for long workdays.",
                    Price = 2199m,
                    ImageUrl = "/images/lenovo-thinkpad.jpg",
                    Category = "Laptops",
                    IsFeatured = true,
                    StockQuantity = 10
                },
                new Product
                {
                    Name = "ASUS ROG Strix G16",
                    Brand = "ASUS",
                    ShortSpecs = "Ryzen 9 • 32GB RAM • RTX 4070",
                    Description = "A serious gaming laptop with a high refresh-rate display and desktop-class graphics power.",
                    Price = 2799m,
                    ImageUrl = "/images/asus-rog.jpg",
                    Category = "Laptops",
                    IsFeatured = true,
                    StockQuantity = 8
                },
                new Product
                {
                    Name = "Samsung Galaxy Book4",
                    Brand = "Samsung",
                    ShortSpecs = "Intel i5 • 16GB RAM • 512GB SSD",
                    Description = "A slim, everyday laptop with a bright AMOLED display and all-day battery life.",
                    Price = 1399m,
                    ImageUrl = "/images/samsung-galaxybook.jpg",
                    Category = "Laptops",
                    IsFeatured = false,
                    StockQuantity = 16
                },
                new Product
                {
                    Name = "Apple iPad Pro 12.9\"",
                    Brand = "Apple",
                    ShortSpecs = "Apple M2 • 256GB • Wi-Fi",
                    Description = "A powerful, portable tablet for creative work, note-taking, and entertainment on the go.",
                    Price = 1599m,
                    ImageUrl = "/images/ipad-pro.jpg",
                    Category = "Tablets",
                    IsFeatured = true,
                    StockQuantity = 22
                },
                new Product
                {
                    Name = "Corsair K95 RGB Keyboard",
                    Brand = "Corsair",
                    ShortSpecs = "Mechanical • Cherry MX • RGB",
                    Description = "A tournament-grade mechanical keyboard with per-key RGB lighting and dedicated macro keys.",
                    Price = 199m,
                    ImageUrl = "/images/corsair-keyboard.jpg",
                    Category = "Accessories",
                    IsFeatured = false,
                    StockQuantity = 30
                },
                new Product
                {
                    Name = "Sony WH-1000XM5",
                    Brand = "Sony",
                    ShortSpecs = "Wireless • Industry-leading noise cancelling",
                    Description = "Premium over-ear headphones with class-leading noise cancellation and all-day comfort.",
                    Price = 399m,
                    ImageUrl = "/images/sony-headphones.jpg",
                    Category = "Accessories",
                    IsFeatured = true,
                    StockQuantity = 25
                },
                new Product
                {
                    Name = "Dell UltraSharp 27\" 4K",
                    Brand = "Dell",
                    ShortSpecs = "4K UHD • USB-C • IPS panel",
                    Description = "A colour-accurate 4K monitor with USB-C connectivity — great for creative work and productivity.",
                    Price = 599m,
                    ImageUrl = "/images/dell-monitor.jpg",
                    Category = "Monitors",
                    IsFeatured = false,
                    StockQuantity = 14
                },
                new Product
                {
                    Name = "Razer Basilisk V3",
                    Brand = "Razer",
                    ShortSpecs = "Wireless • RGB • 26,000 DPI sensor",
                    Description = "A high-precision gaming mouse with customisable weight and eleven programmable buttons.",
                    Price = 89m,
                    ImageUrl = "/images/razer-mouse.jpg",
                    Category = "Accessories",
                    IsFeatured = false,
                    StockQuantity = 35
                },
                new Product
                {
                    Name = "Samsung T7 Portable SSD",
                    Brand = "Samsung",
                    ShortSpecs = "1TB • USB-C • Up to 1,050MB/s",
                    Description = "A pocket-sized external SSD for fast, reliable backups and file transfers on the move.",
                    Price = 129m,
                    ImageUrl = "/images/samsung-ssd.jpg",
                    Category = "Accessories",
                    IsFeatured = false,
                    StockQuantity = 28
                },
                new Product
                {
                    Name = "Logitech Brio 4K Webcam",
                    Brand = "Logitech",
                    ShortSpecs = "4K • HDR • Auto-focus",
                    Description = "A professional-grade webcam for crisp video calls, streaming, and content creation.",
                    Price = 149m,
                    ImageUrl = "/images/logitech-webcam.jpg",
                    Category = "Accessories",
                    IsFeatured = false,
                    StockQuantity = 20
                },
            };

            var existingNames = context.Products.Select(p => p.Name).ToHashSet();
            var toAdd = catalog.Where(p => !existingNames.Contains(p.Name)).ToList();

            if (toAdd.Count > 0)
            {
                context.Products.AddRange(toAdd);
                context.SaveChanges();
            }

            // Bootstrap the very first admin: if nobody has admin rights yet, hand it
            // to whichever account was registered first. This runs on every startup
            // but is a no-op once someone is already an admin. To make a *different*
            // account the admin instead, just flip IsAdmin off/on for the right rows
            // directly (or clear it from everyone and restart to re-bootstrap).
            if (!context.Users.Any(u => u.IsAdmin))
            {
                var firstUser = context.Users.OrderBy(u => u.Id).FirstOrDefault();
                if (firstUser != null)
                {
                    firstUser.IsAdmin = true;
                    context.SaveChanges();
                }
            }
        }
    }
}
