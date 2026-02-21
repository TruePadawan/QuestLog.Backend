using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestLog.Backend.Database;
using QuestLog.Backend.Lib;
using QuestLog.Backend.Lib.Dtos;
using QuestLog.Backend.Models;
using QuestLog.Backend.Settings;
using Resend;

namespace QuestLog.Backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth");

        authGroup.MapPost("/login",
                async Task<IResult> (SignInManager<User> signInManager, LoginRequest request) =>
                {
                    var result = await signInManager.PasswordSignInAsync(
                        request.Email,
                        request.Password,
                        isPersistent: true,
                        lockoutOnFailure: true
                    );

                    if (result.Succeeded)
                    {
                        var response = ApiResponse<object>.Ok(null, "Login successful");
                        return TypedResults.Json(response, statusCode: StatusCodes.Status200OK);
                    }

                    // Email is not verified
                    if (result.IsNotAllowed)
                    {
                        var response = ApiResponse<object>.Fail("Email not verified");
                        return TypedResults.Json(response, statusCode: StatusCodes.Status403Forbidden);
                    }

                    // Handle lockout
                    if (result.IsLockedOut)
                    {
                        var response = ApiResponse<object>.Fail("Too many failed login attempts, try again later");
                        return TypedResults.Json(response, statusCode: StatusCodes.Status423Locked);
                    }

                    return TypedResults.Json(ApiResponse<object>.Fail(), statusCode: StatusCodes.Status401Unauthorized);
                })
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces(403)
            .Produces(423);

        authGroup.MapPost("/register",
                async Task<IResult> (UserManager<User> userManager, IResend resendClient,
                    RegisterRequest request, IOptions<QuestLogSettings> options) =>
                {
                    var user = new User
                    {
                        Email = request.Email,
                        UserName = request.Email
                    };
                    var result = await userManager.CreateAsync(user, request.Password);
                    if (!result.Succeeded)
                    {
                        var response = ApiResponse<object>.Fail("Validation errors",
                            result.Errors.Select(e => e.Description).ToList());
                        return TypedResults.Json(response, statusCode: StatusCodes.Status400BadRequest);
                    }

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    var settings = options.Value;
                    var callbackUrl = GetVerificationUrl(settings.FrontEndUrl, user.Id, code);

                    var emailMessage = GetVerificationEmailMessage(request.Email, callbackUrl);

                    await resendClient.EmailSendAsync(emailMessage);
                    var okResponse = ApiResponse<object>.Ok(null, "Registration successful");
                    return TypedResults.Json(okResponse, statusCode: StatusCodes.Status200OK);
                })
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(400);

        authGroup.MapPost("/resendConfirmationEmail",
                async Task<IResult> (UserManager<User> userManager,
                    IResend resendClient,
                    IOptions<QuestLogSettings> options,
                    ResendConfirmationEmailRequest request) =>
                {
                    var user = await userManager.FindByEmailAsync(request.Email);
                    if (user == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail(),
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var settings = options.Value;
                    var callbackUrl = GetVerificationUrl(settings.FrontEndUrl, user.Id, code);

                    var emailMessage = GetVerificationEmailMessage(request.Email, callbackUrl);
                    await resendClient.EmailSendAsync(emailMessage);

                    return TypedResults.Json(ApiResponse<object>.Ok(null, "Email resent successfully"),
                        statusCode: StatusCodes.Status200OK);
                })
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(400);

        authGroup.MapGet("/confirmEmail",
                async Task<IResult> (UserManager<User> userManager, QuestLogDbContext dbContext, string userId,
                    string code) =>
                {
                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Invalid email confirmation url"),
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail(), statusCode: StatusCodes.Status404NotFound);
                    }

                    string token;
                    try
                    {
                        var decodedBytes = WebEncoders.Base64UrlDecode(code);
                        token = Encoding.UTF8.GetString(decodedBytes);
                    }
                    catch
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Invalid token format"),
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    var result = await userManager.ConfirmEmailAsync(user, token);
                    if (result.Succeeded)
                    {
                        return TypedResults.Json(ApiResponse<object>.Ok(null, "Email confirmed successfully"),
                            statusCode: StatusCodes.Status200OK);
                    }

                    return TypedResults.Json(
                        ApiResponse<object>.Fail("Failed to confirm email",
                            result.Errors.Select(e => e.Description).ToList()),
                        statusCode: StatusCodes.Status400BadRequest);
                })
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(200);

        authGroup.MapPost("/forgotPassword", async Task<IResult> (UserManager<User> userManager,
                ForgotPasswordRequest request,
                IResend resendClient,
                IOptions<QuestLogSettings> options) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return TypedResults.Json(ApiResponse<object>.Fail(), statusCode: StatusCodes.Status400BadRequest);
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var settings = options.Value;
                var callbackUrl = GetPasswordResetUrl(settings.FrontEndUrl, request.Email, code);

                var emailMessage = GetPasswordResetEmailMessage(request.Email, callbackUrl);
                await resendClient.EmailSendAsync(emailMessage);
                return TypedResults.Json(ApiResponse<object>.Ok(null, "Password reset link sent successfully"),
                    statusCode: StatusCodes.Status200OK);
            })
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(200);

        authGroup.MapPost("/resetPassword",
                async Task<IResult> (ResetPasswordRequest request, UserManager<User> userManager) =>
                {
                    var user = await userManager.FindByEmailAsync(request.Email);
                    if (user == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail(),
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    string token;
                    try
                    {
                        var decodedBytes = WebEncoders.Base64UrlDecode(request.ResetCode);
                        token = Encoding.UTF8.GetString(decodedBytes);
                    }
                    catch
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Invalid token format"),
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
                    if (!result.Succeeded)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Validation errors",
                            result.Errors.Select(e => e.Description).ToList()));
                    }

                    return TypedResults.Json(ApiResponse<object>.Ok(null, "Password reset successful"),
                        statusCode: StatusCodes.Status200OK);
                })
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(400);

        authGroup.MapGet("/me", async Task<IResult> (QuestLogDbContext dbContext, ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return TypedResults.Json(ApiResponse<object>.Fail("User not logged in"),
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                var adventurer = await dbContext.Adventurers
                    .Include(a => a.CharacterClass)
                    .ThenInclude(characterClass => characterClass.Progressions)
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (adventurer == null)
                {
                    return TypedResults.Json(ApiResponse<object>.Fail("No adventurer found for this user"),
                        statusCode: StatusCodes.Status404NotFound);
                }

                var adventurerDto = new AdventurerDetailsDto
                {
                    CharacterName = adventurer.CharacterName,
                    CharacterClass = adventurer.CharacterClass.Name,
                    Xp = adventurer.Xp,
                    Tier = Utilities.CalculateTier(adventurer.Xp, adventurer.CharacterClass.Progressions)
                };

                return TypedResults.Json(ApiResponse<AdventurerDetailsDto>.Ok(adventurerDto),
                    statusCode: StatusCodes.Status200OK);
            }).RequireAuthorization()
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(200);

        authGroup.MapPost("/logout", async (SignInManager<User> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.Json(ApiResponse<object>.Ok(null, "Logout successful"),
                    statusCode: StatusCodes.Status200OK);
            })
            .Produces<ApiResponse<object>>(200);

        authGroup.MapGet("/login/google",
            (string returnUrl, LinkGenerator linkGenerator, SignInManager<User> signInManager, HttpContext context) =>
            {
                // Request a redirect to the external login provider.
                // Keep track of the intended return URL for when the external login provider redirects back
                var properties = signInManager.ConfigureExternalAuthenticationProperties("Google",
                    linkGenerator.GetPathByName(context, "GoogleLoginCallback") + $"?returnUrl={returnUrl}");

                // Start external google login flow
                return TypedResults.Challenge(properties, ["Google"]);
            });

        // Google auth redirects back here
        authGroup.MapGet("/login/google/callback",
                async Task<IResult> (string returnUrl, HttpContext context, UserManager<User> userManager,
                    SignInManager<User> signInManager) =>
                {
                    var result = await context.AuthenticateAsync(IdentityConstants.ExternalScheme);
                    if (!result.Succeeded)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Authentication failed"),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    var principal = result.Principal;
                    if (principal == null)
                    {
                        return TypedResults.Json(
                            ApiResponse<object>.Fail("Authentication failed, Claims Principal is null"),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    var email = principal.FindFirstValue(ClaimTypes.Email);
                    if (email == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Authentication failed, Email is null"),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    // Create user if they don't exist
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        // Create user
                        var newUser = new User { UserName = email, Email = email, EmailConfirmed = true };
                        var createResult = await userManager.CreateAsync(newUser);
                        if (!createResult.Succeeded)
                        {
                            return TypedResults.Json(ApiResponse<object>.Fail("Failed to create user",
                                    createResult.Errors.Select(e => e.Description).ToList()),
                                statusCode: StatusCodes.Status400BadRequest);
                        }

                        user = newUser;
                    }

                    var providerKey = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                    var loginInfo = new UserLoginInfo("Google", providerKey, "Google");

                    // Fetch the user's existing external logins from the database
                    var existingLogins = await userManager.GetLoginsAsync(user);

                    // Check if the Google login is already linked
                    var isAlreadyLinked = existingLogins.Any(l =>
                        l.LoginProvider == loginInfo.LoginProvider &&
                        l.ProviderKey == loginInfo.ProviderKey);

                    // Only add the login if it hasn't been linked yet
                    if (!isAlreadyLinked)
                    {
                        var loginResult = await userManager.AddLoginAsync(user, loginInfo);
                        if (!loginResult.Succeeded)
                        {
                            return TypedResults.Json(data: ApiResponse<object>.Fail(message: "Failed to add login",
                                    loginResult.Errors.Select(e => e.Description).ToList()),
                                statusCode: StatusCodes.Status400BadRequest);
                        }
                    }

                    await signInManager.SignInAsync(user, true);
                    // delete temporary cookie used during external authentication
                    await context.SignOutAsync(IdentityConstants.ExternalScheme);

                    return TypedResults.Redirect(returnUrl);
                })
            .WithName("GoogleLoginCallback");
    }

    private static EmailMessage GetVerificationEmailMessage(string email, string callbackUrl)
    {
        return new EmailMessage
        {
            From = "QuestLog <onboarding@notifications.melaninaccessories.live>",
            To = email,
            Subject = "Verify your QuestLog Account",
            HtmlBody = GetVerificationEmailBody(callbackUrl),
            // Fallback for old clients
            TextBody = $"Please verify your account by visiting this link: {callbackUrl}"
        };
    }

    private static EmailMessage GetPasswordResetEmailMessage(string email, string callbackUrl)
    {
        return new EmailMessage
        {
            From = "QuestLog <security@notifications.melaninaccessories.live>",
            To = email,
            Subject = "Reset your QuestLog password",
            HtmlBody = GetPasswordResetEmailBody(callbackUrl),
            // Fallback for old clients
            TextBody = $"Reset your password by visiting this link: {callbackUrl}"
        };
    }

    private static string GetVerificationEmailBody(string callbackUrl)
    {
        return $$"""
                     <!DOCTYPE html>
                     <html>
                     <head>
                         <style>
                             body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                             .container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 8px; }
                             .header { text-align: center; margin-bottom: 30px; }
                             .header h1 { color: #0070f3; margin: 0; } /* Standard Blue or your Brand Green */
                             .button { display: inline-block; padding: 12px 24px; background-color: #0070f3; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }
                             .footer { margin-top: 30px; font-size: 12px; color: #888; text-align: center; }
                             .link-text { word-break: break-all; color: #0070f3; }
                         </style>
                     </head>
                     <body>
                         <div class='container'>
                             <div class='header'>
                                 <h1>Welcome to QuestLog</h1>
                             </div>
                             <p>Greetings, Hero!</p>
                             <p>You are one step away from starting your adventure. Please verify your email address to activate your account.</p>
                             
                             <div style='text-align: center; margin: 30px 0;'>
                                 <a href='{{callbackUrl}}' class='button' style='color: white;'>Verify Email Address</a>
                             </div>

                             <p>This link will expire in <strong>24 hours</strong>.</p>
                             
                             <p>If you did not create this account, you can safely ignore this email.</p>
                             
                             <div class='footer'>
                                 <p>Button not working? Copy and paste this link into your browser:</p>
                                 <p><a href='{{callbackUrl}}' class='link-text'>{{callbackUrl}}</a></p>
                             </div>
                         </div>
                     </body>
                     </html>
                 """;
    }

    private static string GetPasswordResetEmailBody(string callbackUrl)
    {
        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <style>
                         body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
                         .container { max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
                         .header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px; }
                         .header h1 { color: #d946ef; margin: 0; font-size: 24px; } /* Using a 'Magic/Quest' Purple/Pink */
                         .content { padding: 0 10px; }
                         .button-container { text-align: center; margin: 35px 0; }
                         .button { display: inline-block; padding: 14px 28px; background-color: #d946ef; color: white; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px; transition: background-color 0.3s; }
                         .button:hover { background-color: #c026d3; }
                         .warning { background-color: #fff3cd; color: #856404; padding: 10px; border-radius: 4px; font-size: 14px; margin-top: 20px; border-left: 4px solid #ffeeba; }
                         .footer { margin-top: 40px; font-size: 12px; color: #888; text-align: center; border-top: 1px solid #eee; padding-top: 20px; }
                         .link-text { word-break: break-all; color: #d946ef; font-size: 12px; }
                     </style>
                 </head>
                 <body>
                     <div class='container'>
                         <div class='header'>
                             <h1>QuestLog Security</h1>
                         </div>
                         <div class='content'>
                             <p>Hello,</p>
                             <p>We received a request to reset the password for your QuestLog account. No changes have been made to your account yet.</p>
                             
                             <p>You can reset your password by clicking the link below:</p>
                             
                             <div class='button-container'>
                                 <a href='{{callbackUrl}}' class='button' style='color: white;'>Reset Your Password</a>
                             </div>

                             <div class='warning'>
                                 <strong>Security Notice:</strong> This link will expire in <strong>1 hour</strong>. If you did not request a password reset, please ignore this email or contact support if you are concerned.
                             </div>
                             
                             <div class='footer'>
                                 <p>Button not working? Copy and paste this link into your browser:</p>
                                 <p><a href='{{callbackUrl}}' class='link-text'>{{callbackUrl}}</a></p>
                                 <p>&copy; {{DateTime.Now.Year}} QuestLog. All rights reserved.</p>
                             </div>
                         </div>
                     </div>
                 </body>
                 </html>
                 """;
    }

    private static string GetVerificationUrl(string frontEndUrl, string userId, string code)
    {
        return $"{frontEndUrl}/auth/verify-email?userId={userId}&code={code}";
    }

    private static string GetPasswordResetUrl(string frontEndUrl, string userEmail, string resetCode)
    {
        return $"{frontEndUrl}/auth/reset-password?userEmail={userEmail}&code={resetCode}";
    }
}