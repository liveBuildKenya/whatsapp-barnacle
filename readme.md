# Working with twilio for whatsapp
**This example represents the algorithm to a shopping cart experience over the whatsapp api provided by [twilio](https://www.twilio.com)**

## File structure

[Services folder](./Services): Contains the example business logic without the underlying domain object.

[WhatsappController.cs](./WhatsappController.cs): Contains the entry point for the customers' message. It processes the request and sends a response to the Twilio servers which in tern sends a response to the messaging client.

[Whatsapp Bot Service](./services/WhatsappBotService.cs): Contains the implementation of the processing of the shopping cart business logic.