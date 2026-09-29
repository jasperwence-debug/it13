using System;
using System.Net;
using App.Domain.Models.Email;

namespace App.Infrastructure.Services.Email
{
    /// <summary>
    /// Generates high-conversion, professional HTML & Plain Text Win-Back email templates.
    /// Compliant with major desktop, mobile, and web email clients (Gmail, Outlook, Apple Mail).
    /// </summary>
    public static class WinBackEmailTemplate
    {
        public static (string htmlBody, string plainTextBody) Render(WinBackEmailRequest request, string companyName = "CleanPro Operations")
        {
            var customerName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(request.RecipientName) ? "Valued Client" : request.RecipientName);
            var promoCode = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(request.PromoCode) ? "WINBACK10" : request.PromoCode.Trim());
            var discount = request.DiscountPercentage > 0 ? request.DiscountPercentage : 10;
            var daysInactive = request.DaysInactive ?? 30;
            var lastService = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(request.LastServiceType) ? "professional cleaning" : request.LastServiceType);
            var customMsg = !string.IsNullOrWhiteSpace(request.CustomMessage)
                ? $"<p style=\"margin: 16px 0; font-size: 14px; line-height: 1.6; color: #334155; background-color: #f1f5f9; padding: 12px 16px; border-left: 3px solid #0284c7; border-radius: 4px;\">{WebUtility.HtmlEncode(request.CustomMessage)}</p>"
                : string.Empty;

            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>We Miss You at {companyName}!</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f8fafc;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            color: #1e293b;
            -webkit-font-smoothing: antialiased;
        }}
        .container {{
            max-width: 600px;
            margin: 24px auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            border: 1px solid #e2e8f0;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
        }}
        .header {{
            background-color: #0f172a;
            padding: 28px 32px;
            text-align: center;
        }}
        .brand-title {{
            color: #ffffff;
            font-size: 22px;
            font-weight: 700;
            letter-spacing: -0.5px;
            margin: 0;
        }}
        .brand-subtitle {{
            color: #94a3b8;
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-top: 4px;
        }}
        .hero {{
            padding: 32px 32px 20px 32px;
            text-align: left;
        }}
        .hero-title {{
            font-size: 20px;
            font-weight: 700;
            color: #0f172a;
            margin-top: 0;
            margin-bottom: 12px;
        }}
        .hero-text {{
            font-size: 15px;
            line-height: 1.6;
            color: #475569;
            margin: 0 0 16px 0;
        }}
        .voucher-card {{
            background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
            border: 2px dashed #cbd5e1;
            border-radius: 8px;
            padding: 24px;
            margin: 24px 0;
            text-align: center;
        }}
        .voucher-label {{
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            color: #0284c7;
            margin-bottom: 6px;
        }}
        .voucher-code {{
            display: inline-block;
            font-family: 'Courier New', Courier, monospace;
            font-size: 26px;
            font-weight: 700;
            letter-spacing: 3px;
            color: #0f172a;
            background-color: #ffffff;
            padding: 8px 24px;
            border-radius: 6px;
            border: 1px solid #cbd5e1;
            margin: 8px 0;
        }}
        .voucher-desc {{
            font-size: 13px;
            color: #64748b;
            margin-top: 6px;
            margin-bottom: 0;
        }}
        .cta-container {{
            text-align: center;
            margin: 28px 0 16px 0;
        }}
        .cta-button {{
            display: inline-block;
            background-color: #0284c7;
            color: #ffffff !important;
            font-size: 15px;
            font-weight: 600;
            text-decoration: none;
            padding: 14px 32px;
            border-radius: 6px;
            box-shadow: 0 2px 4px rgba(2, 132, 199, 0.25);
        }}
        .perks {{
            background-color: #f8fafc;
            border-top: 1px solid #e2e8f0;
            border-bottom: 1px solid #e2e8f0;
            padding: 20px 32px;
        }}
        .perks-grid {{
            display: table;
            width: 100%;
        }}
        .perk-item {{
            display: table-cell;
            width: 33.33%;
            text-align: center;
            padding: 8px 6px;
            vertical-align: top;
        }}
        .perk-title {{
            font-size: 12px;
            font-weight: 700;
            color: #1e293b;
            margin-top: 4px;
            margin-bottom: 2px;
        }}
        .perk-desc {{
            font-size: 11px;
            color: #64748b;
            margin: 0;
        }}
        .footer {{
            padding: 24px 32px;
            text-align: center;
            font-size: 12px;
            color: #94a3b8;
            line-height: 1.5;
        }}
        .footer a {{
            color: #0284c7;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <!-- Brand Header -->
        <div class=""header"">
            <div class=""brand-title"">{companyName}</div>
            <div class=""brand-subtitle"">Professional Facilities & Home Care</div>
        </div>

        <!-- Hero Content -->
        <div class=""hero"">
            <h1 class=""hero-title"">We Miss You, {customerName}!</h1>
            <p class=""hero-text"">
                It has been <strong>{daysInactive} days</strong> since your last <strong>{lastService}</strong> service with us. 
                Our team takes immense pride in keeping your environment pristine, healthy, and revitalized.
            </p>
            <p class=""hero-text"">
                To show our appreciation for your continued loyalty, we have created an exclusive retention voucher exclusively for your account:
            </p>

            {customMsg}

            <!-- Promo Voucher Block -->
            <div class=""voucher-card"">
                <div class=""voucher-label"">★ Exclusive Return Client Perk</div>
                <div class=""voucher-code"">{promoCode}</div>
                <p class=""voucher-desc"">
                    Receive <strong>{discount}% OFF</strong> your next residential, commercial, or deep cleaning booking.<br>
                    <span style=""font-size: 11px; color: #94a3b8;"">Valid for 30 days. Mention code during booking or dispatch.</span>
                </p>
            </div>

            <!-- Call to Action -->
            <div class=""cta-container"">
                <a href=""tel:09171234567"" class=""cta-button"">Book Your Next Service Now</a>
            </div>
        </div>

        <!-- Value Pillars -->
        <div class=""perks"">
            <div class=""perks-grid"">
                <div class=""perk-item"">
                    <div style=""font-size: 18px;"">🛡️</div>
                    <div class=""perk-title"">100% Satisfaction</div>
                    <div class=""perk-desc"">Complimentary re-touch if you are not delighted</div>
                </div>
                <div class=""perk-item"">
                    <div style=""font-size: 18px;"">✨</div>
                    <div class=""perk-title"">Hospital-Grade</div>
                    <div class=""perk-desc"">Eco-friendly sanitization & certified technicians</div>
                </div>
                <div class=""perk-item"">
                    <div style=""font-size: 18px;"">⚡</div>
                    <div class=""perk-title"">Priority Scheduling</div>
                    <div class=""perk-desc"">Same-day and flexible slots reserved for returning clients</div>
                </div>
            </div>
        </div>

        <!-- Footer -->
        <div class=""footer"">
            <p style=""margin: 0 0 6px 0;"">
                Questions or special service requests? Contact your Dedicated Account Care Team:
            </p>
            <p style=""margin: 0 0 12px 0;"">
                <strong>Hotline:</strong> 0917-123-4567 | <strong>Email:</strong> <a href=""mailto:support@cleanpro-crm.ph"">support@cleanpro-crm.ph</a><br>
                <strong>Service Hubs:</strong> Davao City &bull; Metro Manila &bull; Cebu &bull; Samal
            </p>
            <p style=""margin: 0; font-size: 11px; color: #cbd5e1;"">
                &copy; {DateTime.UtcNow.Year} {companyName}. All rights reserved. You are receiving this message because you are a valued client.
            </p>
        </div>
    </div>
</body>
</html>";

            var plainText = $@"{companyName} — Professional Facilities & Home Care
==============================================================

Dear {request.RecipientName},

We noticed it has been {daysInactive} days since your last {lastService} service with us!

To welcome you back and thank you for being a valued client, here is an exclusive {discount}% discount promo code for your next booking:

--------------------------------------------------------------
PROMO CODE: {promoCode}
DISCOUNT:   {discount}% OFF on your next service
VALIDITY:   Valid for 30 days from today
--------------------------------------------------------------

{(!string.IsNullOrWhiteSpace(request.CustomMessage) ? $"Special Note: {request.CustomMessage}\n\n" : "")}How to redeem:
1. Contact our customer care hotline at 0917-123-4567 or reply to this email.
2. Mention promo code '{promoCode}' during scheduling.
3. Enjoy a fresh, spotless, and sanitized space with our 100% Satisfaction Guarantee.

Our Service Hubs:
Davao City | Metro Manila | Cebu City | Samal

Best regards,
{companyName} Customer Care & Retention Team
Hotline: 0917-123-4567
Email: support@cleanpro-crm.ph
";

            return (html, plainText);
        }
    }
}
