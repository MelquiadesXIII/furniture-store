using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using API.Furnistore.API.Configuration;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Auth;
using API.Furnistore.Shared.Common;
using API.Furnistore.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly IEmailSender _emailSender;
        private readonly APIFurnistoreContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AuthenticationController(
            UserManager<IdentityUser> userManager,
            IOptions<JwtConfig> jwtConfig,
            IEmailSender emailSender,
            APIFurnistoreContext context,
            TokenValidationParameters tokenValidationParameters
        )
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig.Value;
            _emailSender = emailSender;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            // Verifica si el email existe
            var emailExists = await _userManager.FindByEmailAsync(request.EmailAddress);

            if (emailExists != null)
                return BadRequest(
                    new AuthResult()
                    {
                        Result = false,
                        Errors = new List<String>() { "Email already exists" },
                    }
                );

            // Crear usuario
            var user = new IdentityUser()
            {
                Email = request.EmailAddress,
                UserName = request.EmailAddress,
                EmailConfirmed = false,
            };

            var isCreated = await _userManager.CreateAsync(user, request.Password);

            if (isCreated.Succeeded)
            {
                try
                {
                    await SendVerificationEmail(user);
                }
                catch (Exception)
                {
                    return Ok(
                        new AuthResult()
                        {
                            Result = true,
                            Errors = new List<string>()
                            {
                                "User created successfully, but there was an error sending the verification email. Please try to confirm your email later."
                            }
                        }
                    );
                }

                return Ok(new AuthResult() { Result = true });
            }
            else
            {
                var errors = new List<string>();
                foreach (var err in isCreated.Errors)
                    errors.Add(err.Description);

                return BadRequest(new AuthResult { Result = false, Errors = errors });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            // Chequear si el usuario existe
            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            if (existingUser == null)
                return BadRequest(
                    new AuthResult()
                    {
                        Errors = new List<string> { "Invalid Payload" },
                        Result = false,
                    }
                );

            if (!existingUser.EmailConfirmed)
                return BadRequest(
                    new AuthResult()
                    {
                        Errors = new List<string> { "Email needs to be confirmed." },
                        Result = false,
                    }
                );

            var checkUserAndPass = await _userManager.CheckPasswordAsync(
                existingUser,
                request.Password
            );

            if (!checkUserAndPass)
            {
                return BadRequest(
                    new AuthResult()
                    {
                        Errors = new List<string> { "Invalid Credentials" },
                        Result = false,
                    }
                );
            }

            var token = await GenerateTokenAsync(existingUser);

            return Ok(token);
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(
                    new AuthResult
                    {
                        Errors = new List<string> { "Invalid Parameters" },
                        Result = false,
                    }
                );

            var results = await VerifyAndGenerateTokenAsync(tokenRequest);

            if (!results.Result)
                return BadRequest(new AuthResult { Errors = new List<string> { "Invalid Token" } });

            return Ok(results);
        }

        private async Task<AuthResult> VerifyAndGenerateTokenAsync(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var tokenValidationParameters = _tokenValidationParameters.Clone();
                tokenValidationParameters.ValidateLifetime = false;

                var tokenBeingVerified = jwtTokenHandler.ValidateToken(
                    tokenRequest.Token,
                    _tokenValidationParameters,
                    out var validatedToken
                );

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase
                    );

                    if (!result || tokenBeingVerified == null)
                        throw new Exception("Invalid Token");
                }

                var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t =>
                    t.Token == tokenRequest.RefreshToken
                );

                if (storedToken == null)
                    throw new Exception("Invalid Token");

                if (storedToken.IsUsed || storedToken.IsRevoked)
                    throw new Exception("Invalid Token");

                if (storedToken.ExpiryDate < DateTime.UtcNow)
                    throw new Exception("Expired Token");

                var jti = tokenBeingVerified
                    .Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)
                    .Value;

                if (jti != storedToken.JwtId)
                    throw new Exception("Invalid Token");

                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);

                if (dbUser == null)
                    throw new Exception("Invalid Token");

                return await GenerateTokenAsync(dbUser);
            }
            catch (Exception e)
            {
                var message =
                    e.Message == "Invalid Token" || e.Message == "Expired Token"
                        ? e.Message
                        : "Internal Server Error";

                return new AuthResult
                {
                    Result = false,
                    Errors = new List<string> { message },
                };
            }
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return BadRequest(
                    new AuthResult
                    {
                        Errors = new List<string> { "Invalid email confirmation url" },
                        Result = false,
                    }
                );

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound($"Unable to load user with ID '{userId}'.");

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            var result = await _userManager.ConfirmEmailAsync(user, code);

            var status = result.Succeeded
                ? "Thanks you for confirming your email."
                : "There has been an error confirming your email";

            return Ok(status);
        }

        private async Task<AuthResult> GenerateTokenAsync(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim("Id", user.Id),
                            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
                            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                            //  JWT ID, se usa para prevenir ataques de volver a utilizar el token, se agrega un identificador unico
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            // Iat identifica la hora y el dia a la que fue emitida el token
                            new Claim(
                                JwtRegisteredClaimNames.Iat,
                                DateTime.Now.ToUniversalTime().ToString()
                            ),
                        }
                    )
                ),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTime),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                ),
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                Token = RandomGenerator.GenerateRandomString(23),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6), // El estandar es 30 dias
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id,
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                Result = true,
            };
        }

        private async Task SendVerificationEmail(IdentityUser user)
        {
            var verificationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            verificationCode = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(verificationCode)
            );

            var callbackUrl =
                $"{Request.Scheme}://{Request.Host}{Url.Action("ConfirmEmail", controller: "Authentication", new { userId = user.Id, code = verificationCode })}";

            var emailBody =
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email", emailBody);
        }
    }
}
