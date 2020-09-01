using Twilio.AspNet.Common;
using Twilio.TwiML;

namespace ShoppingCart.Webhook.Services
{
    /// <summary>
    /// Interface of the whatsapp bot service
    /// </summary>
    public interface IWhatsappBotService
    {
        MessagingResponse Response(SmsRequest message);
    }
}
