using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Wits_API.DB_Banner;

namespace Wits_API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    [Authorize] // Applies authentication to all endpoints in this controller
    public class BannerController : ControllerBase
    {
        private readonly DB_Banner _dbContext;
        private readonly ILogger<BannerController> _logger;
        private readonly IConfiguration _configuration;

        public BannerController(DB_Banner dbContext, ILogger<BannerController> logger, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: api/Banner
        [HttpGet]
        [AllowAnonymous] // Allows unauthenticated access
        public async Task<IActionResult> GetBanners()
        {
            try
            {
                var banners = await _dbContext.GetBannersAsync();
                if (banners == null || banners.Count == 0)
                    return NotFound("No available banners found.");

                return Ok(banners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving banners.");
                return StatusCode(500, "An error occurred while retrieving the banners.");
            }
        }


        // GET: api/Banner/{bannerId}
        [HttpGet("{bannerId}")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetBannerById(string bannerId)
        {
            try
            {
                var banner = await _dbContext.GetBannerByIdAsync(bannerId);
                if (banner == null)
                    return NotFound($"Banner with ID {bannerId} not found.");

                return Ok(banner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving banner with ID {bannerId}.");
                return StatusCode(500, "An error occurred while retrieving the banner.");
            }
        }

        // POST: api/Banner
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddBanner([FromBody] DB_Banner.Banner_DTO bannerDTO)
        {
            if (bannerDTO == null)
                return BadRequest("Banner data is required.");

            try
            {
                var addedBanner = await _dbContext.AddBannerAsync(bannerDTO);
                return CreatedAtAction(nameof(GetBannerById), new { bannerId = addedBanner.banner_id }, addedBanner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding banner.");
                return StatusCode(500, "An error occurred while adding the banner.");
            }
        }

        // PUT: api/Banner
        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBanner([FromBody] DB_Banner.Banner_DTO bannerDTO)
        {
            if (bannerDTO == null)
                return BadRequest("Banner ID is required for updating.");

            try
            {
                var updated = await _dbContext.UpdateBannerAsync(bannerDTO);
                if (!updated)
                    return NotFound($"Banner with ID {bannerDTO.banner_id} not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating banner with ID {bannerDTO.banner_id}.");
                return StatusCode(500, "An error occurred while updating the banner.");
            }
        }

        // DELETE: api/Banner/{bannerId}
        [HttpDelete("{bannerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBanner(string bannerId)
        {
            try
            {
                var deleted = await _dbContext.DeleteBannerByIdAsync(bannerId);
                if (!deleted)
                    return NotFound($"Banner with ID {bannerId} not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting banner with ID {bannerId}.");
                return StatusCode(500, "An error occurred while deleting the banner.");
            }
        }


        private string GenerateJwtToken(string bannerid)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, bannerid),
            new Claim(ClaimTypes.Role, "User"), // Or "Admin" depending on your logic
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }

}
