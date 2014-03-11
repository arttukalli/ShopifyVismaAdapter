using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Configuration;
using System.Globalization;

using ShopifyAPIAdapterLibrary;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.PricelistMgmtLibrary;
using Nova.SalesOrderMgmtLibrary;
using Nova.InvoiceMgmtLibrary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShopifyVismaApp
{
    class Adapter
    {

        DataSetTableAdapters.ShopTableAdapter shopTA;
        DataSetTableAdapters.CustomerTableAdapter customerTA;
        DataSetTableAdapters.ProductTableAdapter productTA;
        DataSetTableAdapters.OrderTableAdapter orderTA;

        Shopify shop;
        Visma visma;

        public bool UpdateRecords(int shopID) {

            shopTA = new DataSetTableAdapters.ShopTableAdapter();
            customerTA = new DataSetTableAdapters.CustomerTableAdapter();
            productTA = new DataSetTableAdapters.ProductTableAdapter();
            orderTA = new DataSetTableAdapters.OrderTableAdapter();


            // Initialize connections to Shopify and Visma
            shop = Shopify.GetShopByID(shopID);

            if (shop == null)
            {
                LogError(string.Format("Unable to load shop {0} from database", shopID), null);
                return false;
            }

            shop.ProcessShopData();

            visma = new Visma(shop.data.VismaCompany);

            DateTime? lastShopifyUpdateDate = null;
            try
            {
                lastShopifyUpdateDate = shop.data.ShopifyUpdatedDate;
            }
            catch (StrongTypingException ex) { }

            DateTime? lastVismaUpdateDate = null;
            try
            {
                lastVismaUpdateDate = shop.data.VismaUpdatedDate;
            }
            catch (StrongTypingException ex) { }

            DateTime currentUpdateDate = DateTime.Now;

            Log(string.Format("\n== Starting new update for Shop {0} [{1}] at {2}. ==", shop.account, shopID, currentUpdateDate));
            Log(string.Format("Last Shopify update: {0}, Last Visma update: {1}\n", lastShopifyUpdateDate, lastVismaUpdateDate));

            StatusUpdate(string.Format("Starting new update for shop {0} [{1}].", shop.account, shopID));

            try
            {

                // Update Customers
                bool resultCustomers = UpdateCustomers(lastVismaUpdateDate, 0);

                // Update Products
                bool resultProducts = UpdateProducts(lastVismaUpdateDate, 0, true);

                // Update Specific Prices
                bool resultPrices = UpdateSpecificPrices(lastVismaUpdateDate, 50, 0);

                // Update Orders
                bool resultOrders = UpdateOrders(lastShopifyUpdateDate, 0);

                // Update Shop update times
                UpdateShop(currentUpdateDate, currentUpdateDate);
            }
            catch (Exception ex)
            {
                LogError(string.Format("Error updating shop {0}", shop.account), ex);
                StatusUpdate("Error updating shop.");
                return false;
            }


            Log(string.Format("\n=== Finished update at {0}.===\n\n", DateTime.Now) );
            StatusUpdate("Update finished.");

            return true;
        }

        public bool UpdateCustomers(DateTime? startDate, int customerLimit = 0)
        {
            
            int customerCount = 0;

            CustomerList customers = visma.GetCustomerList(startDate);
            Log(string.Format("\n{0} customers found in Visma database.", customers.Count));

            StatusUpdate(string.Format("Updating {0} customers.", customers.Count));
            

            foreach (CustomerInfo customerInfo in customers)
            {
                int customerNumber = customerInfo.Number;

                // Get full Customer object from Visma
                Customer customer = visma.GetCustomerByNumber(customerNumber);

                Customer invoiceCustomer = null;
                if (customer.InvoiceCustomerID > 0)
                    invoiceCustomer = visma.GetCustomerByNumber(customer.InvoiceCustomerID);

                int contactNumber = 0;

                foreach (Contact contact in customer.Contacts)
                {
                    contactNumber++;

                    Log(string.Format("- Customer {0} - {1} [{2}]", customer.FullName, contactNumber.ToString(), customer.Number));

                    // Save article as Shopify Product
                    string email = contact.Email;


                    if (!string.IsNullOrEmpty(email)) //   && (customerNumber == 28)
                    {

                        DataSet.CustomerDataTable customerDT = customerTA.GetDataByCustomerNumber(shop.ID, customerNumber, contactNumber);

                        try
                        {

                            if (customerDT.Count == 0)
                            {
                                string data = shop.GetCustomertDataFromVismaCustomer(customer, contact, invoiceCustomer, null);
                                Log(data);
                                object response = shop.CreateCustomer(data);

                                //Log("Response " + response.ToString());
                                JObject customerResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                                long shopifyID = 0;
                                long addressID = 0;
                                long.TryParse(customerResponse["customer"]["id"].ToString(), out shopifyID);
                                long.TryParse(customerResponse["customer"]["default_address"]["id"].ToString(), out addressID);
                                Log(string.Format(" - Shopify Customer [{0}] created.", shopifyID));

                                customerTA.InsertCustomer(shop.ID, shopifyID, customer.Number, addressID, contactNumber);
                            }
                            else
                            {

                                long shopifyID = customerDT[0].ShopifyCustomerID;
                                long addressID = customerDT[0].ShopifyAddressID;
                                string data = shop.GetCustomertDataFromVismaCustomer(customer, contact, invoiceCustomer, addressID);
                                //Log(data);
                                object response = shop.UpdateCustomer(data, shopifyID);

                                Log(string.Format(" - Shopify Customer [{0}] updated.", shopifyID));
                            }
                        }
                        catch (System.Net.WebException ex)
                        {
                            LogError("Unable to create or update Customer", ex);
                        }

                    }
                    else
                    {
                        Log(" - Unable to create customer - No email value");
                    }
                }
                
       

                customerCount += 1;

                if ((customerLimit > 0) && (customerCount >= customerLimit))
                    break;

            }
            

            return true;
        }



        public bool UpdateProducts(DateTime? startDate, int articleLimit = 0, bool updateArticleImages = false)
        {
            int articlesCount = 0;
            int articleUpdates = 0;
          

            // Get a list of Visma Articles
            ArticleList list = visma.GetArticleList(startDate);
            Log(string.Format("\n{0} products found in Visma database.", list.Count));

            StatusUpdate(string.Format("Updating {0} products.", list.Count));

            
            foreach (ArticleInfo articleInfo in list)
            {
                long? shopifyProductID = null;
                long? shopifyVariantID = null;
                long? shopifyVariantVatID = null;
                long? shopifyImageID = null;

                string articleCode = articleInfo.ArticleCode;
                articlesCount += 1;

                // Get full Article object from Visma
                Article article = visma.GetArticleByCode(articleCode);

                string familyCode = article.FamilyCode;
                string articleName = null;
                decimal? vatRate = null;

                //Log(article.ProductGroupObject.Description.ToString());

                if ((shop.articleTypes.Contains(article.ArticleType))) // && (article.FamilyCode == "532057")
                {

                    Log(string.Format(" - Article {0} [{1}]", articleInfo.ArticleName, articleInfo.ArticleCode));
               

                    // Save article as Shopify Product
                    if (!string.IsNullOrEmpty(article.ArticleName))
                    {

                        if (!string.IsNullOrEmpty(article.FamilyCode))
                        {
                            articleName = visma.GetCommonArticleNameForFamilyCode(article.FamilyCode);
                        }

                        vatRate = visma.GetVatRate(article.ArticleCode);

                        DataSet.ProductDataTable productDT = productTA.GetDataByArticleCode(shop.ID, articleCode);
                        if (productDT.Count == 0)
                        {

                            // Check if article has FamilyCode and if we already have a Shopify product with such FamilyCode
                            if (!string.IsNullOrEmpty(familyCode))
                            {
                                DataSet.ProductDataTable productFamilyDT = productTA.GetDataByFamilyCode(shop.ID, familyCode);

                                if (productFamilyDT.Count > 0)
                                {
                                    // Use existing Shopify Product ID and add this article as its variant
                                    shopifyProductID = productFamilyDT[0].ShopifyProductID;
                                }
                            }

                            if (shopifyProductID == null)
                            {
                                // Create article as a new Shopify Product

                                try
                                {

                                    DateTime? deliveryDate = visma.GetDeliveryDate(article.ArticleCode);
                                    //Log(" - Delivery date: " + deliveryDate.ToString());

                                    shop.CreateUpdateProduct(article, ref shopifyProductID, ref shopifyVariantID, ref shopifyVariantVatID, deliveryDate, articleName, vatRate);
                                    Log(string.Format(" - Shopify Product [{0}] created.", shopifyProductID));

                                    //DataSet.CustomerDataTable customerDT = new DataSet.CustomerDataTable();

                                    productTA.InsertProduct(shop.ID, shopifyProductID, shopifyVariantID, shopifyVariantVatID, articleCode, null, null, null, null, familyCode, deliveryDate);
                                    articleUpdates += 1;
                                }
                                catch (System.Net.WebException ex)
                                {
                                    LogError("Unable to create Shopify Product", ex);
                                }
                            }
                            else
                            {
                                // Create article as a new Variant of existing Shopify Product (with the same FamilyCode)

                                try
                                {
                                    shop.CreateUpdateProductVariants(article, shopifyProductID.Value, ref shopifyVariantID, ref shopifyVariantVatID, null, null, null, null, articleName);

                                    Log(string.Format(" - Shopify Product Variant [{0}] created.", shopifyVariantID));

                                    productTA.InsertProduct(shop.ID, shopifyProductID, shopifyVariantID, shopifyVariantVatID, articleCode, null, null, null, null, familyCode, null);

                                }
                                catch (System.Net.WebException ex)
                                {
                                    LogError("Unable to create Shopify Product Variant", ex);
                                }
                            }
                        }
                        else
                        {
                            //string productData = shop.GetProductDataFromVismaArticle(article, shopifyVariantID, shopifyVariantVatID);

                            try
                            {
                                shopifyProductID = productDT[0].ShopifyProductID;
                                shopifyVariantID = productDT[0].ShopifyVariantID;
                                shopifyVariantVatID = productDT[0].ShopifyVariantVatID;
                                shopifyImageID = productDT[0].ShopifyImageID;

                                DateTime? deliveryDate = visma.GetDeliveryDate(article.ArticleCode);
                                //Log(" - Delivery date: " + deliveryDate.ToString());

                                shop.CreateUpdateProduct(article, ref shopifyProductID, ref shopifyVariantID, ref shopifyVariantVatID, deliveryDate, articleName, vatRate);

                                Log(string.Format(" - Shopify Product [{0}] updated.", shopifyProductID));

                                articleUpdates += 1;
                            }
                            catch (System.Net.WebException ex)
                            {
                                LogError("Unable to update Shopify Product", ex);
                            }

                            
                            //Log(" - Shopify Product already exists.");
                        }

                        // Product image
                        if ((shopifyProductID != null) && (updateArticleImages) && ((shopifyImageID == null) || (shopifyImageID == 0)))
                        {
                            string imagePath = visma.GetImageFilePath(articleCode);
                            if (File.Exists(imagePath))
                            {
                                Log("   - Image exists: " + imagePath);

                                System.IO.FileStream inFile;
                                byte[] binaryData;
                                string base64String;

                                try
                                {
                                    inFile = new System.IO.FileStream(imagePath,
                                                              System.IO.FileMode.Open,
                                                              System.IO.FileAccess.Read);
                                    binaryData = new Byte[inFile.Length];
                                    long bytesRead = inFile.Read(binaryData, 0,
                                                         (int)inFile.Length);
                                    inFile.Close();

                                    base64String = System.Convert.ToBase64String(binaryData, 0, binaryData.Length);

                                    string productImageData = shop.GetProductImageData(article, base64String);

                                    //Log(productImageData);


                                    try
                                    {
                                        object response = shop.CreateProductImage(shopifyProductID.Value, productImageData);
                                        //Log("Response " + response.ToString());

                                        JObject productResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                                        long imageID;
                                        long.TryParse(productResponse["image"]["id"].ToString(), out imageID);
                                        shopifyImageID = imageID;
                                        Log(string.Format("  - New product image created {0}", shopifyImageID));

                                        if (shopifyImageID > 0)
                                        {
                                            productTA.UpdateProductImageID(shopifyImageID, shop.ID, shopifyProductID);
                                        }
  
                                    }
                                    catch (System.Net.WebException ex)
                                    {
                                        LogError("Unable to create product Image", ex);
                                    }

                                    
                                }
                                catch (System.Exception exp)
                                {
                                    // Error creating stream or reading from it.
                                    Log(string.Format("Unable to open image file - {0}", exp.Message));
          
                                }

                            }
                            
                        }
                    }
                    else
                    {
                        Log(" - Unable to create product - No name value");
                    }

                }



                if ((articleLimit > 0) && (articlesCount >= articleLimit))
                    break;
            }
         
            return true;

        }            



        public bool UpdateSpecificPrices(DateTime? startDate, int pricelistrPricesLimit = 50, int customerPricesLimit = 0) {

            
            // Pricelist specific prices

            int pricelistPricesCount = 0;
            Log("\nLoading Pricelist specific prices.");
            StatusUpdate("Updating pricelist specific prices.");

            for (int pricelistNumber = 0; pricelistNumber <= pricelistrPricesLimit; pricelistNumber++) {

                try
                {
                    GeneralPricelist generalPricelist = GeneralPricelist.Read(visma._context, pricelistNumber);

                    var filteredPricelist = generalPricelist.Where(x => ((!startDate.HasValue) || (x.EditStamp >= startDate.Value)));

                    if (filteredPricelist.Count() > 0)
                    {
                        Log(string.Format("Pricelist {0} has {1} specific prices.", pricelistNumber, generalPricelist.Count));

                        foreach (PricelistItem pricelistItem in filteredPricelist)
                        {
                            UpdatePricelistItem(pricelistItem, pricelistNumber, null);
                        }
                    }
                    else
                    {
                        //Log(string.Format("No specific prices for Customer {0}.", customerSpecificNumber));
                    }
                }
                catch (Csla.DataPortalException ex)
                {
                    Log(string.Format("Pricelist {0} - no pricelist", pricelistNumber));
                }
            }
             



            // Customer specific prices

            int customerPricesCount = 0;
            Log("\nLoading Customer specific prices");
            StatusUpdate("Updating customer specific prices.");

            DataSet.CustomerDataTable customerRows = customerTA.GetDataByShopID(shop.ID);
            foreach (var customerRow in customerRows)
            {
                int customerSpecificNumber = customerRow.VismaCustomerNumber;

                CustomerPricelist customerSpecificPricelist = CustomerPricelist.Read(visma._context, customerSpecificNumber);

                var filteredPricelist = customerSpecificPricelist.Where(x => ((!startDate.HasValue) || (x.EditStamp >= startDate.Value)));


                if (filteredPricelist.Count() > 0)
                {
                    Log(string.Format("Customer {0} has {1} specific prices.", customerSpecificNumber, filteredPricelist.Count()));

                    foreach (PricelistItem pricelistItem in filteredPricelist)
                    {
                        UpdatePricelistItem(pricelistItem, null, customerSpecificNumber);
                    }
                }
                else
                {
                    //Log(string.Format("No specific prices for Customer {0}.", customerSpecificNumber));
                }

                customerPricesCount += 1;

                if ((customerPricesLimit > 0) && (customerPricesCount >= customerPricesLimit))
                    break;
            }


            return true;
        }

        public bool UpdatePricelistItem(PricelistItem pricelistItem, int? pricelistNumber, int? customerNumber)
        {
            //Log(" -> " + pricelistItem.ProductCode + " (" + pricelistItem.Active.ToString() + " " + pricelistItem.GetIdValue().ToString() + ") " + pricelistItem.BatchSize.ToString() + "ks " + pricelistItem.ContractPrice.ToString());
            decimal price = pricelistItem.ContractPrice;

            string articleCode = pricelistItem.ProductCode;
            int? quantity = (int)pricelistItem.BatchSize;
            if (quantity <= 1)
                quantity = null;


            Article article = visma.GetArticleByCode(articleCode);

            // If pricelist has Discount value, use it to calculate the final price from currect Article price
            if ((pricelistItem.ContractDiscount > 0) && (pricelistItem.ContractDiscount < 100) && (article != null))
                price = Math.Round(article.Price1 * ((100 - pricelistItem.ContractDiscount) / 100), 2);

            // Check if pricelist is valid by looking at the PVM and VOIMASSA fields
            bool datesValid = true;
            DateTime validFromDate;
            DateTime validToDate;
            DateTime.TryParse(pricelistItem.PricelistDate, out validFromDate);
            DateTime.TryParse(pricelistItem.Valid, out validToDate);
            datesValid = ((validFromDate == DateTime.MinValue) || (validFromDate <= DateTime.Now)) && ((validToDate == DateTime.MinValue) || (validToDate >= DateTime.Now));

            bool currencyMatch = true;
            if (!string.IsNullOrEmpty(article.Currency) && !string.IsNullOrEmpty(pricelistItem.Currency))
            {
                if (article.Currency != pricelistItem.Currency)
                    currencyMatch = false;
            }

            if ((article != null) && (datesValid) && (currencyMatch)) //  && (article.ArticleCode == "A0112")
            {
                string pricelistItemName = article.ArticleCode;
                pricelistItemName += pricelistNumber.HasValue ? string.Format(" P{0}", pricelistNumber) : string.Format(" C{0}", customerNumber);
                if (quantity != null)
                    pricelistItemName += string.Format(" Q{0}", quantity);

                string famiyCode = article.FamilyCode;
                string articleName = null;

                if (!string.IsNullOrEmpty(article.FamilyCode))
                {
                    articleName = visma.GetCommonArticleNameForFamilyCode(article.FamilyCode);
                }

                DataSet.ProductDataTable genericProducts = productTA.GetDataByArticleCode(shop.ID, articleCode);

                if (genericProducts.Count > 0)
                {
                    long? shopifyProductID = genericProducts[0].ShopifyProductID;
                    long? shopifyVariantID = null;
                    long? shopifyVariantVatID = null;

                    DataSet.ProductDataTable specificProducts = productTA.GetDataByArticleCodeSpecific(shop.ID, articleCode, pricelistNumber, customerNumber, quantity);


                    if (specificProducts.Count == 0)
                    {

                        Log(" - Create specific price");

                        try
                        {

                            shop.CreateUpdateProductVariants(article, shopifyProductID.Value, ref shopifyVariantID, ref shopifyVariantVatID, pricelistNumber, customerNumber, quantity, price, articleName);
                            Log(string.Format(" - Shopify Product Variant [{0}] created for specific price {1}.", shopifyVariantID, pricelistItemName));

                            productTA.InsertProduct(shop.ID, shopifyProductID, shopifyVariantID, shopifyVariantVatID, articleCode, pricelistNumber, customerNumber, quantity, price, famiyCode, null);


                        }
                        catch (System.Net.WebException ex)
                        {
                            LogError(string.Format("Unable to create specific price {0}.", pricelistItemName), ex);
                            
                        }


                    }
                    else
                    {
                        shopifyProductID = specificProducts[0].ShopifyProductID;
                        shopifyVariantID = specificProducts[0].ShopifyVariantID;
                        shopifyVariantVatID = specificProducts[0].ShopifyVariantVatID;

                        try
                        {

                            shop.CreateUpdateProductVariants(article, shopifyProductID.Value, ref shopifyVariantID, ref shopifyVariantVatID, pricelistNumber, customerNumber, quantity, price, articleName);
                            Log(string.Format(" - Shopify Product Variant [{0}] updated for specific price {1}.", shopifyVariantID, pricelistItemName));

                        }
                        catch (System.Net.WebException ex)
                        {
                            LogError(string.Format("Unable to update specific price {0}.", pricelistItemName), ex);
                        }
                    }
                }
                else
                {
                    //Log("  - Product not in Shopify");
                }
            }

            return true;
        }

       

        public bool UpdateOrders(DateTime? startDate, int orderLimit = 0)
        {

            Log("\nLoading Orders from Shopify");
            object ordersJson = shop.GetOrders(startDate);
            //Log(ordersJson.ToString());

            JObject ordersJ = JsonConvert.DeserializeObject<JObject>(ordersJson.ToString());
            List<ShopifyOrder> orders = JsonConvert.DeserializeObject<List<ShopifyOrder>>(ordersJ["orders"].ToString());

            Log(string.Format("{0} orders loaded.", orders.Count));
            StatusUpdate(string.Format("Updating {0} orders.", orders.Count));


            for (int i = 0; i < orders.Count; i++)
            {


                ShopifyOrder order = orders[i];

                DataSet.OrderDataTable orderDT = orderTA.GetDataByOrderID(shop.ID, order.id);

                if ((orderDT.Count == 0)) // && (order.order_number == 1008)
                {

                    int salesOrderType = 0;
                    // Order Type - different values for pending and paid
                    if (order.financial_status == "pending")
                        salesOrderType = shop.data.OrderTypePending;
                    else
                        salesOrderType = shop.data.OrderType;

                    SalesOrder sales = SalesOrder.CreateNew(visma._context, 0); // Create with type 0 and update it later in the code, otherwise we might get exception from VismaSDK
                    sales.YourOrderNumber = order.name;
                    sales.OrderDate = order.created_at;
                    sales.OrderType = salesOrderType;

                    
                    // Terms of Payment
                    int? termsOfPayment = shop.GetValueIDFromShopData(shop.termsOfPayment, order.gateway);
                    if (termsOfPayment.HasValue)
                        sales.TermsOfPaymentId = termsOfPayment.Value;

                    // Delivery Method
                    int? deliveryMethod = order.shipping_lines.Count() > 0 ? shop.GetValueIDFromShopData(shop.deliveryMethods, order.shipping_lines[0].code) : null;
                    if (deliveryMethod.HasValue)
                        sales.DeliveryMethodId = deliveryMethod.Value;
                     

                    //Log(string.Format("  {0} > {1}", order.gateway, termsOfPayment.ToString()));
                    //Log(string.Format("  {0} > {1}", order.shipping_lines.Count() > 0 ? order.shipping_lines[0].code : "", deliveryMethod.ToString()));

                    // Customer
                    Customer orderCustomer = null;
                    if (order.customer != null)
                    {
                        string customerTags = order.customer.tags;
                        //Log(customerTags);
                        if (!string.IsNullOrEmpty(customerTags))
                        {
                            string customerTag = customerTags.Split(',').Where(x => x.Trim().StartsWith("+C")).First();
                            customerTag = (!string.IsNullOrEmpty(customerTag)) ? customerTag.Replace("+C", "") : customerTag;
                            int customerID = -1;
                            if (int.TryParse(customerTag, out customerID)) {
                                orderCustomer = visma.GetCustomerByNumber(customerID);
                                sales.CustomerNumber = customerID;
                            }
                            
                        }
                    }

                    sales.TermsOfDeliveryId = shop.data.TermsOfDelivery;
                    sales.SellerId = orderCustomer != null ? orderCustomer.SellerID : shop.data.Seller;

                    // Order delivery date
                    DateTime orderDeliveryDate = order.created_at.AddYears(1);
                    sales.DeliveryDate = orderDeliveryDate;

                    // Unifaun
                    string locationIDText = order.GetNoteAttribute("Unifaun Location ID");
                    int locationID;
                    if (int.TryParse(locationIDText, out locationID)) {
                        sales.DriverId = locationID;
                    }

                    string locationStreetAddress = order.GetNoteAttribute("Unifaun Location Name");
                    string locationCity = order.GetNoteAttribute("Unifaun Location City");


                    // Billing address
                    if (order.billing_address != null)
                    {
                        ShopifyAddress address = order.billing_address;
                        sales.OrdererName = string.Format("{0} {1}", address.first_name, address.last_name).Trim();
                        sales.OrdererStreetAddress = string.Format("{0} {1}", address.address1, address.address2).Trim();
                        sales.OrdererCity = string.Format("{0} {1}", address.zip, address.city).Trim();

                    }

           

                    // Shipping address
                    if (order.shipping_address != null)
                    {
                        ShopifyAddress address = order.shipping_address;
                        sales.DeliveryName = string.Format("{0} {1}", address.first_name, address.last_name).Trim();
                        sales.DeliveryStreetAddress = string.Format("{0} {1}", address.address1, address.address2).Trim();
                        sales.DeliveryCity = string.Format("{0} {1}", address.zip, address.city).Trim();

                        // Unifaun shipping address
                        if (!string.IsNullOrEmpty(locationStreetAddress) && !string.IsNullOrEmpty(locationCity))
                        {
                            sales.DeliveryName2 = "c/o R-Kioski";
                            sales.DeliveryStreetAddress = locationStreetAddress;
                            sales.DeliveryCity = locationCity;
                        }

                    }
                 

                    // Order items
                    foreach (var lineItem in order.line_items)
                    {
                        SalesOrderRow salesRow = sales.SalesorderRows.AddNew();
                        salesRow.ArticleCode = lineItem.sku;
                        salesRow.ArticleName = lineItem.title;
                        salesRow.Amount = lineItem.quantity;
                        salesRow.DeliveryStart = orderDeliveryDate;
                        //salesRow.VatPercent = 0;
                        if (lineItem.price.HasValue)
                            salesRow.UnitPrice = visma.ToPrice(lineItem.price.Value);
                    }
                     


                    if (orderCustomer == null)
                    {
                    }


                    sales.Save();
                    Log(string.Format(" - Sales Order {0} created from order {1}. ", sales.Number, order.name));

                    orderTA.InsertOrder(shop.ID, order.id, sales.Number);


                }
            }

            return true;




        }

        /// <summary>
        /// Update shop update times.
        /// </summary>
        /// <param name="shopifyUpdatedDate">Shopify update time</param>
        /// <param name="vismaUpdatedDate">Visma update time</param>
        public void UpdateShop(DateTime? shopifyUpdatedDate, DateTime? vismaUpdatedDate)
        {
            if (shopifyUpdatedDate.HasValue)
                shopTA.UpdateShopifyUpdatedDate(shopifyUpdatedDate.Value, shop.ID);

            if (vismaUpdatedDate.HasValue)
                shopTA.UpdateVismaUpdatedDate(vismaUpdatedDate.Value, shop.ID);
        }

        /// <summary>
        /// Log string.
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {
            Logged(this, new LoggedEventArgs(text));
            LogMessageToFile(text);
        }

        public void LogError(string text, Exception ex) {
            string errorText = "*** ERROR ****\n" + text;
            if (ex != null)
                errorText += ex.ToString();
            errorText += "**************";

            Log(errorText);
            LogErrorToFile(errorText);
        }

        public void LogMessageToFile(string text)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                string.Format("log_{0}.txt", DateTime.Now.ToString("yyyy_MM_dd")));
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}", System.DateTime.Now, text);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }



        public void LogErrorToFile(string text)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                string.Format("log_errors_{0}.txt", DateTime.Now.ToString("yyyy_MM_dd")));
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}", System.DateTime.Now, text);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }

        /// <summary>
        /// Update status.
        /// </summary>
        /// <param name="text"></param>
        public void StatusUpdate(string text)
        {
            StatusUpdated(this, new StatusUpdatedEventArgs(text));
        }


        public delegate void LoggedEventHandler(object sender, LoggedEventArgs e);
        public event LoggedEventHandler Logged = delegate { };

        public class LoggedEventArgs : EventArgs
        {
            public string text { get; set; }
            public LoggedEventArgs(string text)
                : base()
            {
                this.text = text;
            }
        }

        public delegate void StatusUpdatedEventHandler(object sender, StatusUpdatedEventArgs e);
        public event StatusUpdatedEventHandler StatusUpdated = delegate { };

        public class StatusUpdatedEventArgs : EventArgs
        {
            public string text { get; set; }
            public StatusUpdatedEventArgs(string text)
                : base()
            {
                this.text = text;
            }
        }
    
    }
}
