using BillingSystem.Core.Entities;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Services
{
    public class ShopService : IShopService
    {
        private readonly ApplicationDbContext _context;
        public ShopService(ApplicationDbContext context) { _context = context; }

        public async Task<Shop> GetShopDetailsAsync()
        {
            return await _context.Shops.FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Shop not configured.");
        }

        public async Task<Shop> UpdateShopDetailsAsync(Shop shop)
        {
            var existing = await _context.Shops.FirstOrDefaultAsync();
            if (existing == null) { _context.Shops.Add(shop); }
            else
            {
                existing.ShopName = shop.ShopName;
                existing.OwnerName = shop.OwnerName;
                existing.Address = shop.Address;
                existing.City = shop.City;
                existing.State = shop.State;
                existing.PinCode = shop.PinCode;
                existing.PhoneNumber = shop.PhoneNumber;
                existing.Email = shop.Email;
                existing.GstNumber = shop.GstNumber;
                existing.PanNumber = shop.PanNumber;
                existing.BankName = shop.BankName;
                existing.AccountNumber = shop.AccountNumber;
                existing.IfscCode = shop.IfscCode;
                existing.TermsAndConditions = shop.TermsAndConditions;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return existing ?? shop;
        }
    }
}
