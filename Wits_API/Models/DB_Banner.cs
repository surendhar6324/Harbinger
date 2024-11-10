using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Wits_API
{
    public class DB_Banner : DbContext
    {
        private readonly ILogger<DB_Banner> _logger;

        public DB_Banner(DbContextOptions<DB_Banner> options, ILogger<DB_Banner> logger) : base(options)
        {
            _logger = logger;
        }

        public DbSet<Banner_DTO> banner { get; set; }

        public async Task<List<Banner_DTO>> GetBannersAsync()
        {
            try
            {
                return await banner.Where(r => r.is_available == true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving banners.");
                throw;
            }
        }

        public async Task<Banner_DTO?> GetBannerByIdAsync(string bannerId)
        {
            try
            {
                return await banner.FirstOrDefaultAsync(r => r.banner_id == bannerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving banner with ID {bannerId}.");
                throw;
            }
        }

        public async Task<Banner_DTO> AddBannerAsync(Banner_DTO bannerDTO)
        {
            try
            {
                await banner.AddAsync(bannerDTO);
                await SaveChangesAsync();
                return bannerDTO;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding banner.");
                throw;
            }
        }

        public async Task<bool> UpdateBannerAsync(Banner_DTO bannerDTO)
        {
            try
            {
                var existingBanner = await banner.FirstOrDefaultAsync(x => x.banner_id == bannerDTO.banner_id);
                if (existingBanner == null) return false;

                Entry(existingBanner).CurrentValues.SetValues(bannerDTO);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating banner with ID {bannerDTO.banner_id}.");
                throw;
            }
        }

        public async Task<bool> DeleteBannerByIdAsync(string bannerId)
        {
            try
            {
                var bannersToDelete = await banner.Where(x => x.banner_id == bannerId).ToListAsync();
                if (!bannersToDelete.Any()) return false;

                banner.RemoveRange(bannersToDelete);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting banner with ID {bannerId}.");
                throw;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new InvalidOperationException("Database configuration is required.");
            }
        }

        public class Banner_DTO
        {
            [Key]
            public string banner_id { get; set; }
            public string? image { get; set; }
            public int? resource { get; set; }
            public string? urllink { get; set; }
            public bool? is_available { get; set; }
        }
    }
}
