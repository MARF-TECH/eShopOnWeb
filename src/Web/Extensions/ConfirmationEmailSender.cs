using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.Web.Services
{
    public class ConfirmationEmailSender
    {
        private readonly IEmailSender _emailSender;

        public ConfirmationEmailSender(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }
        
        public Task SendEmailConfirmationAsync(string email, string link)
        {
            return _emailSender.SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }
    }
}
