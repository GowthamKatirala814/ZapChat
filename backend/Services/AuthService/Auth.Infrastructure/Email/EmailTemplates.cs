using System.Net;

namespace Auth.Infrastructure.Email;

/// <summary>
/// The message bodies.
///
/// Two constraints shape these. First, every one carries the code in both an HTML and a
/// plain-text part, because a code that only renders in HTML is invisible in a client
/// that blocks it. Second, nothing internal appears — no user id, no token, no room or
/// tenant identifier — so a forwarded verification email discloses nothing beyond a code
/// that expires in ten minutes.
///
/// The HTML is deliberately plain: tables and inline styles, no external stylesheet, no
/// remote images. Mail clients strip most of what a browser would honour, and a remote
/// image would leak a read receipt to whoever hosts it.
/// </summary>
public static class EmailTemplates
{
    private const string Brand = "ZapChat";

    public static EmailMessage RegistrationCode(
        string toEmail, string fullName, string code, int expiryMinutes, string appUrl) =>
        Code(
            toEmail,
            fullName,
            subject: $"Your {Brand} verification code",
            heading: "Verify your email address",
            intro: $"Use this code to finish setting up your {Brand} account.",
            code: code,
            expiryMinutes: expiryMinutes,
            ignoreLine: $"If you did not sign up for {Brand}, you can ignore this email — " +
                        "no account will be created.",
            appUrl: appUrl);

    public static EmailMessage PasswordResetCode(
        string toEmail, string displayName, string code, int expiryMinutes, string appUrl) =>
        Code(
            toEmail,
            displayName,
            subject: $"Your {Brand} password reset code",
            heading: "Reset your password",
            intro: $"Use this code to choose a new {Brand} password.",
            code: code,
            expiryMinutes: expiryMinutes,
            ignoreLine: "If you did not ask to reset your password, ignore this email. " +
                        "Your current password still works and nothing has changed.",
            appUrl: appUrl);

    /// <summary>A configuration check, sent by the diagnostic command. Carries no code.</summary>
    public static EmailMessage DeliveryTest(string toEmail, string providerName, string endpoint)
    {
        var text =
            $"""
             {Brand} email delivery test

             This message confirms that {Brand} can send email through its configured provider.

             Provider : {providerName}
             Endpoint : {endpoint}

             If you received this, registration and password-reset codes will reach users too.
             """;

        var html = Wrap(
            heading: "Email delivery test",
            bodyHtml:
            $"""
             <p style="{Paragraph}">This message confirms that {Brand} can send email through its
             configured provider.</p>
             <table role="presentation" cellpadding="0" cellspacing="0" style="margin:16px 0;font-size:14px;color:#3f4a60;">
               <tr><td style="padding:2px 12px 2px 0;color:#6b7689;">Provider</td><td>{Encode(providerName)}</td></tr>
               <tr><td style="padding:2px 12px 2px 0;color:#6b7689;">Endpoint</td><td>{Encode(endpoint)}</td></tr>
             </table>
             <p style="{Paragraph}">If you received this, registration and password-reset codes will
             reach users too.</p>
             """,
            footerNote: null);

        return new EmailMessage(toEmail, toEmail, $"{Brand} email delivery test", html, text);
    }

    private static EmailMessage Code(
        string toEmail,
        string displayName,
        string subject,
        string heading,
        string intro,
        string code,
        int expiryMinutes,
        string ignoreLine,
        string appUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "Hello," : $"Hi {displayName},";

        var text =
            $"""
             {greeting}

             {intro}

             Your code is: {code}

             It expires in {expiryMinutes} minutes and can be used once.

             Never share this code. {Brand} staff will never ask you for it.

             {ignoreLine}

             — {Brand}
             {appUrl}
             """;

        var html = Wrap(
            heading: heading,
            bodyHtml:
            $"""
             <p style="{Paragraph}">{Encode(greeting)}</p>
             <p style="{Paragraph}">{Encode(intro)}</p>

             <!-- Letter-spaced monospace: the code has to be readable and re-typable on a
                  phone, where a proportional font makes 0/O and 1/l ambiguous. -->
             <div style="margin:28px 0;padding:20px;background:#f4f6fa;border:1px solid #d5dced;border-radius:10px;text-align:center;">
               <div style="font-size:12px;letter-spacing:.08em;text-transform:uppercase;color:#6b7689;margin-bottom:8px;">
                 Your verification code
               </div>
               <div style="font-family:Consolas,'Courier New',monospace;font-size:34px;font-weight:700;letter-spacing:.28em;color:#0f1729;">
                 {Encode(code)}
               </div>
               <div style="font-size:13px;color:#6b7689;margin-top:10px;">
                 Expires in {expiryMinutes} minutes · single use
               </div>
             </div>

             <p style="{Paragraph}"><strong style="color:#b42318;">Never share this code.</strong>
             {Brand} staff will never ask you for it.</p>

             <p style="{Paragraph};color:#6b7689;font-size:13px;">{Encode(ignoreLine)}</p>
             """,
            footerNote: appUrl);

        return new EmailMessage(toEmail, displayName, subject, html, text);
    }

    private const string Paragraph = "margin:0 0 14px;font-size:15px;line-height:1.55;color:#3f4a60;";

    /// <summary>
    /// The shared frame. Table-based and inline-styled because that is what mail clients
    /// actually render; a colour scheme is not worth a broken layout in Outlook.
    /// </summary>
    private static string Wrap(string heading, string bodyHtml, string? footerNote) =>
        $"""
         <!doctype html>
         <html lang="en">
         <head>
           <meta charset="utf-8">
           <meta name="viewport" content="width=device-width,initial-scale=1">
           <title>{Encode(heading)}</title>
         </head>
         <body style="margin:0;padding:0;background:#eef1f7;">
           <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#eef1f7;padding:32px 12px;">
             <tr>
               <td align="center">
                 <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                        style="max-width:520px;background:#ffffff;border:1px solid #d5dced;border-radius:14px;overflow:hidden;font-family:'Segoe UI',system-ui,-apple-system,sans-serif;">
                   <tr>
                     <td style="padding:22px 28px;border-bottom:1px solid #e6eaf3;">
                       <span style="display:inline-block;width:26px;height:26px;line-height:26px;text-align:center;border-radius:6px;background:#2563eb;color:#ffffff;font-weight:700;font-size:15px;">Z</span>
                       <span style="margin-left:9px;font-size:16px;font-weight:600;color:#0f1729;vertical-align:middle;">{Brand}</span>
                     </td>
                   </tr>
                   <tr>
                     <td style="padding:28px;">
                       <h1 style="margin:0 0 16px;font-size:19px;font-weight:600;color:#0f1729;">{Encode(heading)}</h1>
                       {bodyHtml}
                     </td>
                   </tr>
                   <tr>
                     <td style="padding:16px 28px;background:#f4f6fa;border-top:1px solid #e6eaf3;font-size:12px;color:#6b7689;">
                       This is an automated message from {Brand}. Please do not reply.
                       {(footerNote is null ? "" : $"<br><span style=\"color:#8b95a8;\">{Encode(footerNote)}</span>")}
                     </td>
                   </tr>
                 </table>
               </td>
             </tr>
           </table>
         </body>
         </html>
         """;

    /// <summary>
    /// HTML-encodes interpolated values.
    ///
    /// A name comes from user input at registration, so it reaches this template
    /// unvalidated — encoding is what stops a display name closing a tag and injecting
    /// markup into a message the recipient trusts.
    /// </summary>
    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
