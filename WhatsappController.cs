using ShoppingCart.Webhook.Services;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;

namespace ShoppingCart.WebHook
{
    /// <summary>
    /// Represents Twilio Controller for whatsapp
    /// </summary>
    public class WhatsappController : TwilioController
    {
        private readonly IWhatsappBotService _whatsappBotService;

        public WhatsappController(IWhatsappBotService whatsappBotService)
        {
            this._whatsappBotService = whatsappBotService;
        }
        public TwiMLResult Index(SmsRequest message)
        {
            var response = _whatsappBotService.Response(message);

            return TwiML(response);
        }
    }
}
