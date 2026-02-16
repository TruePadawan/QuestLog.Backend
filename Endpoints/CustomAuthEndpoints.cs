using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using QuestLog.Backend.Database;
using QuestLog.Backend.Settings;
using Resend;

namespace QuestLog.Backend.Endpoints;

public static class CustomAuthEndpoints
{
    public static void MapCustomAuthEndpoints(this WebApplication app)
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
                        return TypedResults.Ok();
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
                    var callbackUrl = $"{settings.FrontEndUrl}/verify-email?userid={user.Id}&code={code}";

                    var emailMessage = GetVerificationEmailMessage(request.Email, callbackUrl);

                    await resendClient.EmailSendAsync(emailMessage);
                    return TypedResults.Ok();
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
                        return TypedResults.BadRequest("No user with specified email address");
                    }

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var settings = options.Value;
                    var callbackUrl = $"{settings.FrontEndUrl}/verify-email?userid={user.Id}&code={code}";

                    var emailMessage = GetVerificationEmailMessage(request.Email, callbackUrl);
                    await resendClient.EmailSendAsync(emailMessage);
                    return TypedResults.Ok();
                })
            .Produces(200)
            .Produces(400);
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
}