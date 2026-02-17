using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using QuestLog.Backend.Database;
using QuestLog.Backend.Lib.Dtos;
using QuestLog.Backend.Settings;
using Resend;

namespace QuestLog.Backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth");

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
                        return TypedResults.Ok(new { Message = "Login successful" });
                    }

                    // Email is not verified
                    if (result.IsNotAllowed)
                    {
                        return TypedResults.Problem("Please check your inbox and verify your email address",
                            statusCode: 403);
                    }

                    // Handle lockout
                    if (result.IsLockedOut)
                    {
                        return TypedResults.Problem("Too many failed login attempts, try again later bozo",
                            statusCode: 423);
                    }

                    return TypedResults.Unauthorized();
                })
            .Produces(200)
            .Produces(401)
            .ProducesProblem(403)
            .ProducesProblem(423);

        authGroup.MapPost("/register",
                async Task<IResult> (UserManager<User> userManager, IResend resendClient,
                    RegisterRequest request, IOptions<QuestLogSettings> options) =>
                {
                    var user = new User
                    {
                        UserName = request.Email, Email = request.Email
                    };
                    var result = await userManager.CreateAsync(user, request.Password);
                    if (!result.Succeeded)
                    {
                        return TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code,
                            e => new[] { e.Description }));
                    }

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    var settings = options.Value;
                    var callbackUrl = GetVerificationUrl(settings.FrontEndUrl, user.Id, code);

                    var emailMessage = GetVerificationEmailMessage(request.Email, callbackUrl);

                    await resendClient.EmailSendAsync(emailMessage);
                    return TypedResults.Ok(new { Message = "Check your email for a verification link" });
                })
            .Produces(200)
            .ProducesValidationProblem();

        authGroup.MapPost("/resendConfirmationEmail",
                async Task<IResult> (UserManager<User> userManager,
                    IResend resendClient,
                    IOptions<QuestLogSettings> options,
                    ResendConfirmationEmailRequest request) =>
                {
                    var user = await userManager.FindByEmailAsync(request.Email);
                    if (user == null)
                    {
                        return TypedResults.BadRequest();
                    }

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var settings = options.Value;
                    var callbackUrl = GetVerificationUrl(settings.FrontEndUrl, user.Id, code);

                    var emailMessage = GetVerificationEmailMessage(request.Email, callbackUrl);
                    await resendClient.EmailSendAsync(emailMessage);
                    return TypedResults.Ok(new { Message = "Check your email for a verification link" });
                })
            .Produces(200)
            .Produces(400);

        authGroup.MapGet("/confirmEmail",
                async Task<IResult> (UserManager<User> userManager, string userId, string code) =>
                {
                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                    {
                        return TypedResults.BadRequest("Invalid email confirmation url");
                    }

                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        return TypedResults.Unauthorized();
                    }

                    string token;
                    try
                    {
                        var decodedBytes = WebEncoders.Base64UrlDecode(code);
                        token = Encoding.UTF8.GetString(decodedBytes);
                    }
                    catch
                    {
                        return TypedResults.BadRequest(new { Message = "Invalid token format" });
                    }

                    var result = await userManager.ConfirmEmailAsync(user, token);
                    if (result.Succeeded)
                    {
                        return TypedResults.Ok(new { Message = "Email verified successfully" });
                    }

                    return TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code,
                        e => new[] { e.Description }));
                })
            .ProducesValidationProblem()
            .Produces(200)
            .Produces(400);

        authGroup.MapPost("/forgotPassword", async Task<IResult> (UserManager<User> userManager,
                ForgotPasswordRequest request,
                IResend resendClient,
                IOptions<QuestLogSettings> options) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return TypedResults.BadRequest();
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var settings = options.Value;
                var callbackUrl = GetPasswordResetUrl(settings.FrontEndUrl, request.Email, code);

                var emailMessage = GetPasswordResetEmailMessage(request.Email, callbackUrl);
                await resendClient.EmailSendAsync(emailMessage);
                return TypedResults.Ok(new { Message = "Password reset link has been sent" });
            })
            .Produces(400)
            .Produces(200);

        authGroup.MapPost("/resetPassword",
                async Task<IResult> (ResetPasswordRequest request, UserManager<User> userManager) =>
                {
                    var user = await userManager.FindByEmailAsync(request.Email);
                    if (user == null)
                    {
                        return TypedResults.BadRequest();
                    }

                    string token;
                    try
                    {
                        var decodedBytes = WebEncoders.Base64UrlDecode(request.ResetCode);
                        token = Encoding.UTF8.GetString(decodedBytes);
                    }
                    catch
                    {
                        return TypedResults.BadRequest(new { Message = "Invalid token format" });
                    }

                    var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
                    if (!result.Succeeded)
                    {
                        return TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code,
                            e => new[] { e.Description }));
                    }

                    return TypedResults.Ok(new { Message = "Password has been reset" });
                })
            .Produces(200)
            .Produces(400)
            .ProducesValidationProblem();

        authGroup.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var userDto = new UserDto
            {
                Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                CharacterName = user.FindFirst(c => c.Type == "CharacterName")?.Value ?? string.Empty
            };
            return TypedResults.Ok(userDto);
        }).RequireAuthorization();

        authGroup.MapPost("/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.Ok(new { Message = "Logout successful" });
        });
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
        return $"{frontEndUrl}/verify-email?userId={userId}&code={code}";
    }

    private static string GetPasswordResetUrl(string frontEndUrl, string userEmail, string resetCode)
    {
        return $"{frontEndUrl}/reset-password?userEmail={userEmail}&code={resetCode}";
    }
}