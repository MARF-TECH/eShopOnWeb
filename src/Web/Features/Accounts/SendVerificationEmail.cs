using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Coravel.Invocable;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Services;

namespace Microsoft.eShopWeb.Web.Features
{
    public class SendVerificationEmailCommand : IRequest
    {
        public ClaimsPrincipal User { get; }
        public string Scheme { get; }
        public HostString RequestHost { get; }

        public SendVerificationEmailCommand(ClaimsPrincipal user, string scheme, HostString requestHost)
        {
            User = user;
            Scheme = scheme;
            RequestHost = requestHost;
        }
    }
    
    public class SendVerificationEmailHandler : IRequestHandler<SendVerificationEmailCommand>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly LinkGenerator _linkGenerator;

        public SendVerificationEmailHandler(UserManager<ApplicationUser> userManager, IBackgroundJobClient backgroundJobClient, LinkGenerator linkGenerator)
        {
            _userManager = userManager;
            _backgroundJobClient = backgroundJobClient;
            _linkGenerator = linkGenerator;
        }
        
        public async Task<Unit> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(request.User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(request.User)}'.");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = _linkGenerator.GetUriByAction("ConfirmEmail", "Manage", new { user.Id, code }, request.Scheme, request.RequestHost);
            
            var email = user.Email;

            _backgroundJobClient.Enqueue<ConfirmationEmailSender>(emailSender => emailSender.SendEmailConfirmationAsync(email, callbackUrl));

            return Unit.Value;
        }
    }
    
    public class SendVerificationInvocable : IInvocableWithPayload<SendVerificationEmailCommand>, IInvocable
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LinkGenerator _linkGenerator;
        private readonly IEmailSender _emailSender;

        public SendVerificationEmailCommand Payload { get; set; }

        public SendVerificationInvocable(UserManager<ApplicationUser> userManager, LinkGenerator linkGenerator, IEmailSender emailSender)
        {
            _userManager = userManager;
            _linkGenerator = linkGenerator;
            _emailSender = emailSender;
        }
        
        public async Task Invoke()
        {
            var user = await _userManager.GetUserAsync(Payload.User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(Payload.User)}'.");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = _linkGenerator.GetUriByAction("ConfirmEmail", "Manage", new { user.Id, code }, Payload.Scheme, Payload.RequestHost);
            
            var email = user.Email;

            await _emailSender.SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>link</a>");
        }
    }
}