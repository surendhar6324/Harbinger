using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;

    public AuthMiddleware(RequestDelegate next, IConfiguration config, ILogger<AuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jwtKey = config["Jwt:Key"];
        _jwtIssuer = config["Jwt:Issuer"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            if (!ValidateToken(token, out var principal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Invalid or expired token.");
                return;
            }

            context.User = principal; // Attach the user to the HttpContext for further use in the pipeline
        }

        await _next(context);
    }

    private bool ValidateToken(string token, out System.Security.Claims.ClaimsPrincipal principal)
    {
        principal = null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtIssuer,
                ValidAudience = _jwtIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Optionally, you could further check the roles here if required
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed.");
            return false;
        }
    }
}
