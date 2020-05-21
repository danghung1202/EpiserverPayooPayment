using System;

namespace Foundation.Commerce.Payment.Payoo
{
    public class PaymentXMLFactory
    {
        private static string _XML = @"<shops>
                                <shop>
                                    <session>{0}</session>
                                    <username>{1}</username>
                                    <shop_id>{2}</shop_id>
                                    <shop_title>{3}</shop_title>
                                    <shop_domain>{4}</shop_domain>
                                    <shop_back_url>{5}</shop_back_url>
                                    <order_no>{6}</order_no>
                                    <order_cash_amount>{7}</order_cash_amount>
                                    <order_ship_date>{8}</order_ship_date>
                                    <order_ship_days>{9}</order_ship_days>
                                    <order_description>{10}</order_description>
                                    <notify_url>{11}</notify_url>
                                    <validity_time>{12}</validity_time>
                                    <customer>
                                       <name>{13}</name>
                                       <phone>{14}</phone>
                                       <address>{15}</address>
                                       <city>{16}</city>
                                       <email>{17}</email>
                                    </customer>
                                </shop>
                            </shops>";
        private static string _XML_Temp = @"<shops>
                                <shop>
                                    <session>{0}</session>
                                    <username>{1}</username>
                                    <shop_id>{2}</shop_id>
                                    <shop_title>{3}</shop_title>
                                    <shop_domain>{4}</shop_domain>
                                    <shop_back_url>{5}</shop_back_url>
                                    <order_no>{6}</order_no>
                                    <order_cash_amount>{7}</order_cash_amount>
                                    <order_ship_date>{8}</order_ship_date>
                                    <order_ship_days>{9}</order_ship_days>
                                    <order_description>{10}</order_description>
                                    <notify_url>{11}</notify_url>
                                    <customer>
                                       <name>{12}</name>
                                       <phone>{13}</phone>
                                       <address>{14}</address>
                                       <city>{15}</city>
                                       <email>{16}</email>
                                    </customer>
                                </shop>
                            </shops>";

        public static string GetPaymentXML(PayooOrder payooOrder)
        {
            try
            {
                if (payooOrder == null)
                    throw new Exception("Parameter is not set.");
                if (string.IsNullOrEmpty(payooOrder.ValidityTime))
                {
                    return string.Format(_XML_Temp, payooOrder.Session, payooOrder.BusinessUsername, payooOrder.ShopID, payooOrder.ShopTitle,
                              payooOrder.ShopDomain, payooOrder.ShopBackUrl, payooOrder.OrderNo, payooOrder.OrderCashAmount, payooOrder.StartShippingDate,
                              payooOrder.ShippingDays, payooOrder.OrderDescription, payooOrder.NotifyUrl,
                              payooOrder.CustomerName, payooOrder.CustomerPhone, payooOrder.CustomerAddress, payooOrder.CustomerCity, payooOrder.CustomerEmail);
                }
                return string.Format(_XML, payooOrder.Session, payooOrder.BusinessUsername, payooOrder.ShopID, payooOrder.ShopTitle,
                              payooOrder.ShopDomain, payooOrder.ShopBackUrl, payooOrder.OrderNo, payooOrder.OrderCashAmount, payooOrder.StartShippingDate,
                              payooOrder.ShippingDays, payooOrder.OrderDescription, payooOrder.NotifyUrl, payooOrder.ValidityTime,
                              payooOrder.CustomerName, payooOrder.CustomerPhone, payooOrder.CustomerAddress, payooOrder.CustomerCity, payooOrder.CustomerEmail);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
