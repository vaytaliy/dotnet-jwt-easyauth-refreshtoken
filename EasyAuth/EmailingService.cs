using EasyAuth.Interfaces;
using EasyAuth.Utils;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyAuth
{
    public class EmailingService
    {
        private readonly string _senderAddress;
        private readonly string _senderPassword;
        private readonly string _smtpHost;
        private readonly string _smtpPort;
        private readonly string _authURL;
        private readonly string _hostURL;
        private readonly string _authority;
        private readonly string _audience;
        private readonly int _emailTokenExpirationMinutes;
        private readonly string _emailSecretAuthKey;
        private readonly bool _validateAudience;
        private readonly bool _validateIssuer;
        private readonly string _verificationPath;
        private readonly string _messageSubject;
        private readonly string _messagePattern;
        private readonly string _recoveryPath;
        private readonly int _recoveryPageURLValidity;
        private readonly string _emailSecretRecoveryKey;
        private readonly string _recoveryMessageSubject;
        private readonly string _recoveryMessagePattern;
        public readonly bool _validateIssuerSigningKey;


        //public AuthTokenizationService(IOptionsMonitor<JwtLibOptions> opts)
        //{
        //    _options = opts.CurrentValue;
        //}
        public EmailingService(
            IConfiguration configuration, IOptionsMonitor<JwtLibOptions> secretsOptions) {

            var secrets = secretsOptions.CurrentValue;

            var authConfigPart = configuration.GetSection("AuthSettings");
            var emailingConfigPart = configuration.GetSection("AuthSettings:Emailing");

            //Secrets
            _emailSecretAuthKey = secrets.EmailSecretAuthKey;
            _senderPassword = secrets.EmailPassword;
            _emailSecretRecoveryKey = secrets.EmailSecretRecoveryKey;

            //Config global
            _authURL = authConfigPart["AuthURL"];
            _hostURL = authConfigPart["BaseURL"];
            _authority = authConfigPart["Authority"];
            _audience = authConfigPart["Audience"];
            _validateAudience = bool.Parse(authConfigPart["ValidationParameters:ValidateAudience"]);
            _validateIssuer = bool.Parse(authConfigPart["ValidationParameters:ValidateIssuer"]);
            _validateIssuerSigningKey = authConfigPart.GetValue("ValidationParameters:ValidateIssuerSigningKey", false);
            
            //Config emailing
            _senderAddress = emailingConfigPart["EmailUser"];
            _smtpHost = emailingConfigPart["SMTPAddress"];
            _smtpPort = emailingConfigPart["SMTPPort"];
            _verificationPath = "verification";
            _recoveryPath = "recovery";
            _messageSubject = emailingConfigPart["MessageSubject"];
            _messagePattern = emailingConfigPart["MessagePattern"];
            _emailTokenExpirationMinutes = emailingConfigPart.GetValue("EmailTokenExpirationMinutes", 10);

            //config emailing reovery
            _recoveryPageURLValidity = emailingConfigPart.GetValue("RecoveryPageURLValidity", 10);
            _recoveryMessageSubject = emailingConfigPart["RecoveryMessageSubject"];
            _recoveryMessagePattern = emailingConfigPart["RecoveryMessagePattern"];
        }

        public enum EmailTypes
        {
            Verification,
            Recovery
        }

        public string GenerateEmailingToken(ClaimsIdentity identity, EmailTypes emailType = EmailTypes.Verification)
        {
            int expiresAfter;
            string secretKey;

            switch (emailType)
            {
                case EmailTypes.Verification:
                    expiresAfter = _emailTokenExpirationMinutes;
                    secretKey = _emailSecretAuthKey;
                    break;
                case EmailTypes.Recovery:
                    expiresAfter = _recoveryPageURLValidity;
                    secretKey = _emailSecretRecoveryKey;
                    break;
                default:
                    throw new ArgumentException("Incorrect argument");
            }


            var jwtAccessTokenObj = new JwtSecurityToken(
                issuer:             _authority,
                audience:           _audience,
                notBefore:          DateTime.UtcNow,
                claims:             identity.Claims,
                expires:            DateTime.UtcNow.AddMinutes(expiresAfter),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256)
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwtAccessTokenObj);
            return token;
        }

        public ClaimsPrincipal ValidateTokenGetPrincipal(string token, EmailTypes emailType = EmailTypes.Verification)
        {
            string secretKey = emailType switch
            {
                EmailTypes.Verification => _emailSecretAuthKey,
                EmailTypes.Recovery => _emailSecretRecoveryKey,
                _ => throw new ArgumentException("Incorrect argument"),
            };
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = _validateAudience,
                ValidateIssuer   = _validateIssuer,
                ValidateLifetime =  true, 
                IssuerSigningKey =  new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                ValidateIssuerSigningKey = _validateIssuerSigningKey,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            JwtSecurityToken jwtSecurityToken = (JwtSecurityToken)securityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }

        public async Task<string> SendRecoveryEmail(string emailTo, string username, ClaimsIdentity claimsIdentity)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Authorization Test", _senderAddress));
            message.To.Add(new MailboxAddress("Recepient", emailTo));
            message.Subject = _recoveryMessageSubject;

            var token = GenerateEmailingToken(claimsIdentity, EmailTypes.Recovery);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var fullURL = $"{_hostURL}/{_authURL}/{_recoveryPath}?recoveryToken={encodedToken}";

            var messageBodyText = _recoveryMessagePattern.Replace("@username", username).Replace("@url", fullURL);


            message.Body = new TextPart("html") { Text = messageBodyText };

            await SendEmailAsync(message);
            return fullURL;
        }

        public async Task SendVerificationEmail(
            string emailTo, string username, DateTime securityStamp, List<string> userRoles)
        {            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Authorization Test", _senderAddress));
            message.To.Add(new MailboxAddress("Recepient", emailTo));
            message.Subject = _messageSubject;

            var ci = AuthTokenizationService.GetIdentity(username, emailTo, securityStamp, false, userRoles);
            var token = GenerateEmailingToken(ci);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var fullURL = $"{_hostURL}/{_authURL}/{_verificationPath}?verificationToken={encodedToken}";

            var messageBodyText = _messagePattern.Replace("@username", username).Replace("@url", fullURL);


            message.Body = new TextPart("html"){Text = messageBodyText};

            await SendEmailAsync(message);

        }

        private async Task SendEmailAsync(MimeMessage message)
        {
            var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(
                    host: _smtpHost, 
                    port: int.Parse(_smtpPort), 
                    options: SecureSocketOptions.Auto);

                await client.AuthenticateAsync(_senderAddress, _senderPassword);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
