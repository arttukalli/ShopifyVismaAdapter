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

using ShopifyAPIAdapterLibrary;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.PricelistMgmtLibrary;
using Nova.SalesOrderMgmtLibrary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShopifyVismaApp
{
    class Adapter
    {
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

        DataSetTableAdapters.ShopTableAdapter shopTA;
        DataSetTableAdapters.CustomerTableAdapter customerTA;
        DataSetTableAdapters.ProductTableAdapter productTA;

        Shopify shop;
        Visma visma;


        public bool UpdateRecords(int shopID) {


            shopTA = new DataSetTableAdapters.ShopTableAdapter();
            customerTA = new DataSetTableAdapters.CustomerTableAdapter();
            productTA = new DataSetTableAdapters.ProductTableAdapter();
         
            DataSet.ShopDataTable shopsData = shopTA.GetDataByID(shopID);
            if (shopsData.Count == 0)
            {
                LogError(string.Format("Unable to load shop {0}", shopID));
                return false;
            }

            // Initialize connections to Shopify and Visma
            shop = new Shopify(shopID, shopsData[0].ShopifyStoreAccount, shopsData[0].ShopifyAccessToken);
            visma = new Visma(shopsData[0].VismaCompany);

            DateTime? lastShopifyUpdateDate = null;
            try
            {
                lastShopifyUpdateDate = shopsData[0].ShopifyUpdatedDate;
            }
            catch (StrongTypingException ex) { }

            DateTime? lastVismaUpdateDate = null;
            try
            {
                lastVismaUpdateDate = shopsData[0].VismaUpdatedDate;
            }
            catch (StrongTypingException ex) { }

            Log(string.Format("\n== Starting new update for Shop {0} [{1}] at {2}. ==", shop.account, shopID, DateTime.Now));
            Log(string.Format("Last Shopify update: {0}, Last Visma update: {1}\n", lastShopifyUpdateDate, lastVismaUpdateDate));


            // Update Customers
            bool resultCustomers = UpdateCustomers(lastVismaUpdateDate, 1);

            // Update Products
            bool resultProducts = UpdateProducts(lastVismaUpdateDate, 100);

            // Update Specific Prices
            bool resultPrices = UpdateSpecificPrices(lastVismaUpdateDate, 0, 1);

            // Update Orders
            //bool resultOrders = UpdateOrders(visma.orderType, lastShopifyUpdateDate, 1);

            Log("\n=== Finished update.===\n\n");

            return true;
        }

        public bool UpdateCustomers(DateTime? startDate, int customerLimit = 0)
        {
            
            int customerCount = 0;

            CustomerList customers = visma.GetCustomerList(startDate);
            Log(string.Format("\n{0} customers found in Visma database.", customers.Count));
            

            foreach (CustomerInfo customerInfo in customers)
            {
                int customerNumber = customerInfo.Number;

                // Get full Customer object from Visma
                Customer customer = visma.GetCustomerByNumber(customerNumber);

                Log(string.Format("- Customer {0} [{1}]", customer.FullName, customer.Number));
      
                // Save article as Shopify Product
                string email = (customer.Contacts.Count > 0) ? customer.Contacts[0].Email : "";

                if (!string.IsNullOrEmpty(email))
                {

                    DataSet.CustomerDataTable customerDT = customerTA.GetDataByCustomerNumber(shop.ID, customerNumber);
                    if (customerDT.Count == 0)
                    {

                        string data = shop.GetCustomertDataFromVismaCustomer(customer);

                        //Log(data.ToString());

                        try
                        {
                            object response = shop.CreateCustomer(data);

                            //Log("Response " + response.ToString());
                            JObject customerResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                            long shopifyID = 0;
                            long.TryParse(customerResponse["customer"]["id"].ToString(), out shopifyID);
                            Log(string.Format(" - Shopify Customer [{0}] created.", shopifyID));

                            customerTA.InsertCustomer(shop.ID, shopifyID, customer.Number);
                        }
                        catch (System.Net.WebException ex)
                        {
                            LogError(ex.ToString());
                        }
                    }
                    else
                    {
                        Log(" - Shopify Customer already exists.");
                    }

                }
                else
                {
                    Log(" - Unable to create customer - No email value");
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

            
            foreach (ArticleInfo articleInfo in list)
            {
                long shopifyProductID = 0;
                long shopifyVariantID = 0;
                long shopifyImageID = 0;

                string articleCode = articleInfo.ArticleCode;
                articlesCount += 1;

                // Get full Article object from Visma
                Article article = visma.GetArticleByCode(articleCode);

                //Log(article.ProductGroupObject.Description.ToString());

                if ((article.ArticleType == 5) || (article.ArticleType == 25) || (article.ArticleType == 44))
                {

                    Log(string.Format(" - Article {0} [{1}]", articleInfo.ArticleName, articleInfo.ArticleCode));
               

                    // Save article as Shopify Product
                    if (!string.IsNullOrEmpty(article.ArticleName))
                    {
                        DataSet.ProductDataTable productDT = productTA.GetDataByArticleCode(shop.ID, articleCode);
                        if (productDT.Count == 0)
                        {

                            string productData = shop.GetProductDataFromVismaArticle(article);

                            //Log(productData.ToString());

                            try
                            {
                                object response = shop.CreateProduct(productData);

                                Log("Response " + response.ToString());
                                JObject productResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                                long.TryParse(productResponse["product"]["id"].ToString(), out shopifyProductID);
                                long.TryParse(productResponse["product"]["variants"][0]["id"].ToString(), out shopifyVariantID);
                                Log(string.Format(" - Shopify Product [{0}] created.", shopifyProductID));

                                //DataSet.CustomerDataTable customerDT = new DataSet.CustomerDataTable();

                                productTA.InsertProduct(shop.ID, shopifyProductID, shopifyVariantID, articleCode, null, null, null, null);
                                articleUpdates += 1;
                            }
                            catch (System.Net.WebException ex)
                            {
                                LogError(ex.ToString());
                            }
                        }
                        else
                        {
                            shopifyProductID = productDT[0].ShopifyProductID;
                            shopifyImageID = productDT[0].ShopifyImageID;
                            Log(" - Shopify Product already exists.");
                        }

                        // Product image
                        if ((shopifyProductID > 0) && (updateArticleImages) && (shopifyImageID == 0))
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
                                        object response = shop.CreateProductImage(shopifyProductID, productImageData);
                                        //Log("Response " + response.ToString());

                                        JObject productResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                                        long.TryParse(productResponse["image"]["id"].ToString(), out shopifyImageID);
                                        Log(string.Format("  - New product image created {0}", shopifyImageID));

                                        if (shopifyImageID > 0)
                                        {
                                            productTA.UpdateProductImageID(shopifyImageID, shop.ID, shopifyProductID);
                                        }
  
                                    }
                                    catch (System.Net.WebException ex)
                                    {
                                        LogError(ex.ToString());
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
            Log("\nLoading Pricelist specific prices");

            for (int pricelistNumber = 0; pricelistNumber <= pricelistrPricesLimit; pricelistNumber++) {

                try
                {
                    GeneralPricelist generalPricelist = GeneralPricelist.Read(visma._context, pricelistNumber);

                    if (generalPricelist.Count > 0)
                    {
                        Log(string.Format("Pricelist {0} has {1} specific prices.", pricelistNumber, generalPricelist.Count));

                        foreach (PricelistItem pricelistItem in generalPricelist)
                        {
                            Log(" -> " + pricelistItem.ProductCode + " (" + pricelistItem.Active.ToString() + " " + pricelistItem.GetIdValue().ToString() + ") " + pricelistItem.BatchSize.ToString() + "ks " + pricelistItem.ContractPrice.ToString());
                            decimal price = pricelistItem.ContractPrice;
                            string articleCode = pricelistItem.ProductCode;
                            int? quantity = (int)pricelistItem.BatchSize;
                            if (quantity <= 1)
                                quantity = null;


                            DataSet.ProductDataTable specificProducts = productTA.GetDataByArticleCodeSpecific(shop.ID, articleCode, pricelistNumber, null, quantity);


                            if (specificProducts.Count == 0)
                            {

                                DataSet.ProductDataTable genericProducts = productTA.GetDataByArticleCode(shop.ID, articleCode);

                                if (genericProducts.Count > 0)
                                {

                                    long shopifyProductID = genericProducts[0].ShopifyProductID;
                                    long shopifyVariantID = 0;
                                    Article article = visma.GetArticleByCode(articleCode);

                                    if (article != null)
                                    {
                                        string productVariantData = shop.GetProductVariantData(article, pricelistNumber, null, quantity, price);
                                        //Log(productVariantData);
                                        Log(" - Create specific price");

                                        try
                                        {
                                            object response = shop.CreateProductVariant(shopifyProductID, productVariantData);

                                            //Log("Response " + response.ToString());
                                            JObject productVariantResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                                            long.TryParse(productVariantResponse["variant"]["id"].ToString(), out shopifyVariantID);
                                            Log(string.Format(" - Shopify Product Variant [{0}] created.", shopifyVariantID));

                                            productTA.InsertProduct(shop.ID, shopifyProductID, shopifyVariantID, articleCode, pricelistNumber, null, quantity, price);

                                        }
                                        catch (System.Net.WebException ex)
                                        {
                                            LogError(ex.ToString());
                                        }
                                    }

                                }
                                else
                                {
                                    Log("  - Product not in Shopify");
                                }

                            }
                            else
                            {
                            }
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

            DataSet.CustomerDataTable customerRows = customerTA.GetDataByShopID(shop.ID);
            foreach (var customerRow in customerRows)
            {
                int customerSpecificNumber = customerRow.VismaCustomerNumber;

                CustomerPricelist customerSpecificPricelist = CustomerPricelist.Read(visma._context, customerSpecificNumber);


                if (customerSpecificPricelist.Count > 0)
                {
                    Log(string.Format("Customer {0} has {1} specific prices.", customerSpecificNumber, customerSpecificPricelist.Count));

                    foreach (PricelistItem pricelistItem in customerSpecificPricelist)
                    {
                        Log(" -> " + pricelistItem.ProductCode + " (" + pricelistItem.Active.ToString() + " " + pricelistItem.GetIdValue().ToString() + ") " + pricelistItem.BatchSize.ToString() + "ks " + pricelistItem.ContractPrice.ToString());
                        decimal price = pricelistItem.ContractPrice;                        
                        string articleCode = pricelistItem.ProductCode;
                        int? quantity = (int)pricelistItem.BatchSize;
                        if (quantity <= 1)
                            quantity = null;


                        DataSet.ProductDataTable specificProducts = productTA.GetDataByArticleCodeSpecific(shop.ID, articleCode, null, customerSpecificNumber, quantity);

                        //Log(specificProducts.Count.ToString());

                        if (specificProducts.Count == 0)
                        {

                            DataSet.ProductDataTable genericProducts = productTA.GetDataByArticleCode(shop.ID, articleCode);

                            if (genericProducts.Count > 0)
                            {

                                long shopifyProductID = genericProducts[0].ShopifyProductID;
                                long shopifyVariantID = 0;
                                Article article = visma.GetArticleByCode(articleCode);

                                if (article != null)
                                {
                                    string productVariantData = shop.GetProductVariantData(article, null, customerSpecificNumber, quantity, price);
                                    //Log(productVariantData);
                                    Log(" - Create specific price");

                                    try
                                    {
                                        object response = shop.CreateProductVariant(shopifyProductID, productVariantData);

                                        //Log("Response " + response.ToString());
                                        JObject productVariantResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                                        long.TryParse(productVariantResponse["variant"]["id"].ToString(), out shopifyVariantID);
                                        Log(string.Format(" - Shopify Product Variant [{0}] created.", shopifyVariantID));

                                        //DataSet.CustomerDataTable customerDT = new DataSet.CustomerDataTable();

                                        productTA.InsertProduct(shop.ID, shopifyProductID, shopifyVariantID, articleCode, null, customerSpecificNumber, quantity, price);
           
                                    }
                                    catch (System.Net.WebException ex)
                                    {
                                        LogError(ex.ToString());
                                    }
                                }

                            }
                            else
                            {
                                Log("  - Product not in Shopify");
                            }
                            
                        }
                        else
                        {
                        }
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

        public bool UpdateOrders(int orderType, DateTime? startDate, int orderLimit = 0)
        {

            Log("\nLoading Orders from Shopify");
            object ordersJson = shop.GetOrders();
            //Log(ordersJson.ToString());

            JObject ordersJ = JsonConvert.DeserializeObject<JObject>(ordersJson.ToString());
            List<ShopifyOrder> orders = JsonConvert.DeserializeObject<List<ShopifyOrder>>(ordersJ["orders"].ToString());

            Log(string.Format("{0} orders loaded.", orders.Count));


            for (int i = 0; i < orders.Count; i++)
            {


                ShopifyOrder order = orders[i];


                SalesOrder sales = SalesOrder.CreateNew(visma._context, 0);
                sales.YourOrderNumber = order.name;
                sales.OrderDate = order.created_at;
                sales.OrderType = orderType;

                // Billing address
                if (order.billing_address != null)
                {
                    ShopifyAddress address = order.billing_address;
                    sales.OrdererName = string.Format("{0} {1}", address.first_name, address.last_name).Trim();
                    sales.OrdererStreetAddress = string.Format("{0} {1}", address.address1, address.address2).Trim();
                    sales.OrdererCity = string.Format("{0} {1}", address.zip, address.city).Trim();
                    
                }

                // Billing address
                if (order.shipping_address != null)
                {
                    ShopifyAddress address = order.shipping_address;
                    sales.DeliveryCity = string.Format("{0} {1}", address.first_name, address.last_name).Trim();
                    sales.DeliveryStreetAddress = string.Format("{0} {1}", address.address1, address.address2).Trim();
                    sales.DeliveryCity = string.Format("{0} {1}", address.zip, address.city).Trim();

                }

                // Order items
                foreach (var lineItem in order.line_items)
                {
                    SalesOrderRow salesRow = sales.SalesorderRows.AddNew();
                    salesRow.ArticleCode = lineItem.sku;
                    salesRow.Amount = lineItem.quantity;
                    if (lineItem.price.HasValue)
                        salesRow.UnitPrice = visma.ToPrice(lineItem.price.Value);
                }


                sales.Save();
                Log(string.Format(" - Sales Order {0} created. ", sales.YourOrderNumber));

            }

            return true;




        }

        /// <summary>
        /// Log string.
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {

            Logged(this, new LoggedEventArgs(text));
        }

        public void LogError(string text) {
            Log("*** ERROR ****");
            Log(text);
            Log("**************");
        }
    
    }
}
