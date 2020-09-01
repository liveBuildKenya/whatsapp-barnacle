using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCart.Webhook.Services;

namespace ShoppingCart.Webhook
{
    /// <summary>
    /// Service collection extensions
    /// </summary>
    public static class ShoppingCartServiceCollectionExtension
    {
        public static void ConfigureShoppingCartWebhookServices(this IServiceCollection services, IConfigurationRoot configuration)
        {
            services.AddTransient<IWhatsappBotService, WhatsappBotService>();            
        }
    }
}
