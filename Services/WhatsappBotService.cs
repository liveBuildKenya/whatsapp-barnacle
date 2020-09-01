using ShoppingCart.Core.Domain.Catalog;
using ShoppingCart.Core.Domain.Orders;
using ShoppingCart.Core.Domain.Users;
using ShoppingCart.Services.Catalog;
using ShoppingCart.Services.Orders;
using ShoppingCart.Services.Users;
using System;
using System.Collections.Generic;
using System.Text;
using Twilio.AspNet.Common;
using Twilio.TwiML;

namespace ShoppingCart.Webhook.Services
{
    /// <summary>
    /// Whatsapp bot implementation
    /// </summary>
    public class WhatsappBotService : IWhatsappBotService
    {
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;

        public WhatsappBotService(IUserService userService,
            IProductService productService,
            IOrderService orderService)
        {
            this._userService = userService;
            this._productService = productService;
            this._orderService = orderService;
        }

        public MessagingResponse Response(SmsRequest message)
        {
            var messageResponse = new MessagingResponse();
            var user = _userService.GetUserByPhoneNumber(message.From);

            #region User

            if (user == null)
            {
                var newUser = new User()
                {
                    PhoneNumber = message.From,
                    CreatedOnUtc = DateTime.UtcNow,
                    CommStage = 1,
                };
                _userService.InsertUser(newUser);

                messageResponse.Message("Hello," + Environment.NewLine +
                    "I am Zuri. Welcome to Ideal a quick mart for your favourite products." + Environment.NewLine +
                    "We would like to save your number to personalize your experience." + Environment.NewLine +
                    "What is your name?");
                return messageResponse;
            }
            else
            {
                #region Start Communication

                if (user.CommStage == 1)
                {
                    var userName = message.Body;

                    messageResponse.Message(string.Format("What can we get for you {0}?", userName));
                    user.Name = userName;
                    user.CommStage = 2;
                    _userService.UpdateUser(user);
                    return messageResponse;
                }

                #endregion

                #region Product Search

                if (user.CommStage == 2)
                {
                    var product = _productService.GetProductByName(message.Body);

                    if (product == null)
                    {
                        var unfoundProduct = new UnfoundProduct()
                        {
                            Name = message.Body,
                            CreatedOnUtc = DateTime.UtcNow
                        };

                        messageResponse.Message(string.Format("Sorry, but we could not find {0}." + Environment.NewLine +
                            "What can we get for you?", unfoundProduct.Name));

                        _productService.InsertUnfoundProduct(unfoundProduct);

                        return messageResponse;
                    }
                    else
                    {
                        var orderStatus = _orderService.GetOrderStatusBySystemName(SystemOrderStatus.Cart);
                        var brands = product.Brands;
                        var order = new Order()
                        {
                            OrderStatus = orderStatus,
                            CreateOnUtc = DateTime.UtcNow,
                            User = user
                        };
                        order.OrderItems.Add(new OrderItem
                        {
                            //ProductName = product.Name,
                            //ModifiedOnUtc = DateTime.UtcNow
                        });

                        _orderService.InsertOrder(order);

                        StringBuilder sb = new StringBuilder("Which one is your preference? " + Environment.NewLine);
                        foreach (var brand in brands)
                        {
                            sb.Append(string.Format("{0} " + Environment.NewLine, brand.Name));
                        }

                        user.CommStage = 3;
                        _userService.UpdateUser(user);

                        messageResponse.Message(sb.ToString());

                        return messageResponse;
                    }
                }

                #endregion

                #region Preference

                if (user.CommStage == 3)
                {
                    var brand = _productService.GetBrandByName(message.Body);
                    var currentOrder = _orderService.GetUserCurrentOrder(user.Id);
                    var orderDetail = _orderService.GetOrderDetail(currentOrder.Id);
                    var unitMeasures = brand.BrandUnitMeasures;

                    if (brand == null)
                    {
                        messageResponse.Message(string.Format("{0} could not be found." + Environment.NewLine +
                            "Make sure you type one of the found brands.", message.Body));
                        return messageResponse;
                    }
                    else
                    {
                        //orderDetail.BrandName = brand.Name;
                        //orderDetail.ModifiedOnUtc = DateTime.UtcNow;

                        _orderService.UpdateOrderDetail(orderDetail);

                        StringBuilder sb = new StringBuilder("What is your prefered unit measure?" + Environment.NewLine);
                        foreach (var unitMeasure in unitMeasures)
                        {
                            var measure = _productService.GetUnitMeasureById(unitMeasure.UnitMeasureId);
                            sb.Append(string.Format("{0} for Kshs. {1}" + Environment.NewLine, measure.Value, unitMeasure.Price));
                        }

                        user.CommStage = 4;
                        _userService.UpdateUser(user);

                        messageResponse.Message(sb.ToString());

                        return messageResponse;
                    }

                }

                #endregion

                #region Unit measure

                if (user.CommStage == 4)
                {
                    var measure = message.Body;
                    var currentOrder = _orderService.GetUserCurrentOrder(user.Id);
                    var orderDetail = _orderService.GetOrderDetail(currentOrder.Id);

                    orderDetail.UnitMeasure = measure;
                    orderDetail.ModifiedOnUtc = DateTime.UtcNow;

                    _orderService.UpdateOrderDetail(orderDetail);

                    user.CommStage = 5;
                    _userService.UpdateUser(user);

                    messageResponse.Message("How many?");
                    return messageResponse;
                }

                #endregion

                #region Quantity

                if (user.CommStage == 5)
                {
                    var quantity = Convert.ToInt32(message.Body);
                    var currentOrder = _orderService.GetUserCurrentOrder(user.Id);
                    var orderDetail = _orderService.GetOrderDetail(currentOrder.Id);
                    //var priceAsOrdered = _productService.GetPriceAsOrdered(orderDetail.BrandName, orderDetail.UnitMeasure);

                    orderDetail.Quantity = quantity;
                    //orderDetail.PriceAsOrdered = priceAsOrdered;
                    orderDetail.ModifiedOnUtc = DateTime.UtcNow;

                    _orderService.UpdateOrderDetail(orderDetail);

                    #region Order Summary

                    StringBuilder sb = new StringBuilder(string.Format("Order number: {0}" + Environment.NewLine, currentOrder.Id));
                    //var orderDetails = currentOrder.OrderDetails;
                    var total = 0;

                    //foreach (var detail in orderDetails)
                    //{
                    //    total = total + (detail.PriceAsOrdered * detail.Quantity);
                    //    sb.Append(string.Format("{0} {1} {2} {3} at Kshs. {4} each" + Environment.NewLine,
                    //        detail.Quantity, detail.UnitMeasure, detail.ProductName, detail.BrandName, detail.PriceAsOrdered));
                    //}

                    sb.Append(string.Format("Total: {0}" + Environment.NewLine, total));
                    sb.Append("Would you like to continue shopping?");

                    currentOrder.Total = total;
                    _orderService.UpdateOrder(currentOrder);

                    #endregion

                    user.CommStage = 6;
                    _userService.UpdateUser(user);

                    messageResponse.Message(sb.ToString());
                    return messageResponse;
                }

                #endregion

                #region Checkout

                if (user.CommStage == 6)
                {
                    var continueShopping = message.Body;
                    if (string.Equals("yes", continueShopping, StringComparison.OrdinalIgnoreCase))
                    {
                        messageResponse.Message(string.Format("What else can we get for you {0}?", user.Name));

                        user.CommStage = 2;
                        _userService.UpdateUser(user);

                        return messageResponse;
                    }

                    if (string.Equals("no", continueShopping, StringComparison.OrdinalIgnoreCase))
                    {
                        if (user.LnmNumber == string.Empty)
                        {
                            user.CommStage = 7;
                            _userService.UpdateUser(user);
                            messageResponse.Message("We support Lipa na MPESA. Which mpesa number would you like for us to send an authorization request for payment?");
                            return messageResponse;
                        }
                        else
                        {
                            user.CommStage = 8;
                            _userService.UpdateUser(user);
                            messageResponse.Message(string.Format("Send authorization request to {0}?", user.LnmNumber));
                            return messageResponse;
                        }
                    }
                }

                #endregion

                #region Lipa na Mpesa

                if (user.CommStage == 7)
                {
                    var lnmNumber = message.Body;
                    user.LnmNumber = lnmNumber;
                    user.CommStage = 8;
                    _userService.UpdateUser(user);
                    messageResponse.Message(string.Format("Send authorization request to {0}?", user.LnmNumber));
                    return messageResponse;
                }

                #region LNM authorization Sending



                #endregion

                #endregion

                #region Delivery
                #endregion
            }

            #endregion

            return messageResponse;
        }
    }
}
