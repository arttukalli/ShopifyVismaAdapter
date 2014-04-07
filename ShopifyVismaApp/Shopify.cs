using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Globalization;


using ShopifyAPIAdapterLibrary;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.PricelistMgmtLibrary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShopifyVismaApp
{
    public class Shopify
    {
        private ShopifyAPIClient _api;
        private string _token;
        public string account;
        private DateTime lastApiCallTime;
        public int ID;
        public DataSet.ShopRow data = null;
        public List<int> articleTypes = null;
        public Dictionary<int, string> termsOfPayment = null;
        public Dictionary<int, string> deliveryMethods = null;

        // Minimum delay between 2 Shopify API calls (to handle Shopify limits of maximum number of requests per second).
        private int apiMinDelay = 600;



        public static Shopify GetShopByID(int ID)
        {
            DataSetTableAdapters.ShopTableAdapter shopTA = new DataSetTableAdapters.ShopTableAdapter();
            DataSet.ShopDataTable shopDT = shopTA.GetDataByID(ID);
            if (shopDT.Count > 0)
            {
                Shopify shop = new Shopify(ID, shopDT[0].ShopifyStoreAccount, shopDT[0].ShopifyAccessToken);
                shop.data = shopDT[0];
                return shop;

                
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Initialize Shopify by creating a Shopify API Client object.
        /// </summary>
        public Shopify(int ID, string account, string token)
        {
            this.ID = ID;
            this.account = account;
            this._token = token;
            this._api = GetAPIClient();
        }

        /// <summary>
        /// Get API Client object with connection to a Shopify store.
        /// </summary>
        /// <returns></returns>
        public ShopifyAPIClient GetAPIClient()
        {
            
            ShopifyAuthorizationState authState = new ShopifyAuthorizationState();
            authState.ShopName = this.account;
            authState.AccessToken = this._token;

            ShopifyAPIClient api = new ShopifyAPIClient(authState);

            return api;
        }

        /// <summary>
        /// Get shop settings and turn them into lists and dictionaries
        /// </summary>
        public void ProcessShopData()
        {
            try
            {
                if (!string.IsNullOrEmpty(data.ArticleTypes))
                    articleTypes = data.ArticleTypes.Split(',').Select(int.Parse).ToList();

                if (!string.IsNullOrEmpty(data.TermsOfPayment))
                {
                    termsOfPayment = data.TermsOfPayment.Split(';').Select(s => s.Split(':')).ToDictionary(a => int.Parse(a[0].Trim()), a => a[1].Trim());
                }

                if (!string.IsNullOrEmpty(data.DeliveryMethod))
                {
                    deliveryMethods = data.DeliveryMethod.Split(';').Select(s => s.Split(':')).ToDictionary(a => int.Parse(a[0].Trim()), a => a[1].Trim());
                }
            }
            catch (StrongTypingException ex) { }

        }

        /// <summary>
        /// Get key by looking for string in dictionary.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public int? GetValueIDFromShopData(Dictionary<int, string> dict, string text) {
            int? value = null;

            if (!string.IsNullOrEmpty(text))
            {
                var keys = dict.Where(kv => text.Contains(kv.Value)).Select(kv => kv.Key);
                if (keys.Count() > 0)
                    value = keys.First();
            }

            return value;
        }



        /// <summary>
        /// Get list of all Shopify products.
        /// </summary>
        /// <returns></returns>
        public object GetProducts()
        {
            return ShopifyGet("/admin/products.json");
        }

        /// <summary>
        /// Get list of all Shopify customers.
        /// </summary>
        /// <returns></returns>
        public object GetCustomers()
        {
            return ShopifyGet("/admin/customers.json");
        }

        /// <summary>
        /// Get list of all Shopify orders.
        /// </summary>
        /// <returns></returns>
        public object GetOrders()
        {
            return ShopifyGet("/admin/orders.json");
        }

        /// <summary>
        /// Creates a new product in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateProduct(object data)
        {
            object response = ShopifyPost("/admin/products.json", data);
            return response;
        }

        public object UpdateProduct(object data, long id)
        {
            object response = ShopifyPut("/admin/products/" + id.ToString() + ".json", data);
            return response;
        }

        /// <summary>
        /// Creates a new product variant in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateProductVariant(long productID, object data)
        {
            string url = string.Format("/admin/products/{0}/variants.json", productID);
            object response = ShopifyPost(url, data);
            return response;
        }

        /// <summary>
        /// Updates a product variant in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object UpdateProductVariant(object data, long variantID)
        {
            string url = string.Format("/admin/variants/{0}.json", variantID);
            object response = ShopifyPut(url, data);
            return response;
        }

        /// <summary>
        /// Creates a new product image in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateProductImage(long productID, object data)
        {
            string url = string.Format("/admin/products/{0}/images.json", productID);
            object response = ShopifyPost(url, data);
            return response;
        }



        /// <summary>
        /// Creates a new customer in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateCustomer(object data)
        {
            object response = ShopifyPost("/admin/customers.json", data);
            return response;
        }

        public object UpdateCustomer(object data, long id)
        {
            object response = ShopifyPut("/admin/customers/" + id.ToString() + ".json", data);
            return response;
        }


        /// <summary>
        /// Get list of Orders from Shopify.
        /// </summary>
        /// <param name="changedDate">Set to retrieve Orders created or modified after this date.</param>
        /// <returns></returns>
        public object GetOrders(DateTime? changedDate)
        {
            string ordersUrl = "/admin/orders.json?limit=250";
            if (changedDate.HasValue)
            {
                ordersUrl += "&updated_at_min=" + changedDate.Value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"); ;
            }
            //var data = { };
            object response = ShopifyGet(ordersUrl);
            return response;
        }

        public object ShopifyGet(string url)
        {
            SyncShopifyRequest();
            LogShopifyToFileRequest("GET", url, null);
            object response = this._api.Get(url);
            LogShopifyToFileResponse(response);
            return response;
        }

        public object ShopifyPost(string url, object data)
        {
            SyncShopifyRequest();
            LogShopifyToFileRequest("POST", url, data);
            object response = this._api.Post(url, data);
            LogShopifyToFileResponse(response);
            return response;
        }


        public object ShopifyPut(string url, object data)
        {
            SyncShopifyRequest();
            LogShopifyToFileRequest("PUT", url, data);
            object response = this._api.Put(url, data);
            LogShopifyToFileResponse(response);
            return response;
        }

        /// <summary>
        /// Add delay between 2 consecutive Shopify calls to avoid hitting Shopify API calls limit.
        /// </summary>
        public void SyncShopifyRequest()
        {
            int timeFromLastCall = DateTime.Now.Subtract(this.lastApiCallTime).Milliseconds;

            if ((timeFromLastCall > 0) && (timeFromLastCall < this.apiMinDelay))
            {
                int delayTime = this.apiMinDelay - timeFromLastCall;
                //LogShopifyToFileResponse(string.Format("Delay: {0}" ,timeFromLastCall));
                System.Threading.Thread.Sleep(delayTime);
            }
            this.lastApiCallTime = DateTime.Now;

            
        }



        public double? ToPrice(decimal number)
        {
            double price;
            string numberString = number.ToString("#.##");
            double.TryParse(numberString, out price);

            return price;
        }

        public double? ToGrams(decimal number)
        {
            double grams;
            number = number * 1000;
            string numberString = number.ToString();
            double.TryParse(numberString, out grams);

            return grams;
        }

        public int ToQuantity(decimal number)
        {
            int qty;
            qty = Convert.ToInt32(number);

            return qty;
        }

        /// <summary>
        /// Converts Visma Article object into Product JSON accepted by SHopify.
        /// </summary>
        /// <param name="article">Visma Article object</param>
        /// <returns>Product JSON string</returns>
        public string GetProductDataFromVismaArticle(Article article, DateTime? deliveryDate, string articleName, decimal? vatRate, string videoUrl, bool includeVariants)
        {

 

            ShopifyProductSimple obj = new ShopifyProductSimple();
            obj.title = string.IsNullOrEmpty(articleName) ? article.ArticleName : articleName;
            //obj.body_html = "";
            obj.product_type = "Product";
            obj.published = (article.Points != 3);

            string tags = string.Format("+T{0}", article.ArticleType);

            if (article.Points == 1)
            {
                tags += ",Order";
            }

            ProductGroup articleProductGroup = null;
            try
            {
                articleProductGroup = article.ProductGroupObject;
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                // Note: article.ProductGroup sometimes throws an exception, ignore it.
            }
            if ((articleProductGroup != null) && (!string.IsNullOrEmpty(articleProductGroup.Description)))
            {
                tags += (string.IsNullOrEmpty(tags)) ? "" : ",";
                tags += "\"" + article.ProductGroupObject.Description + "\"";
            }

            // VAT Rate as tag
            if (vatRate.HasValue)
            {
                tags += string.Format(",+V{0}", vatRate.Value.ToString("0.##"));
            }

            // Delivery date as tag
            if (deliveryDate != null)
            {
                tags += string.Format(",+D{0}", deliveryDate.Value.ToString("yyyy-MM-dd"));
            }

            obj.tags = tags;

            // Video URL as metafield
            List<Metafield> metafields = new List<Metafield>();
            if (!string.IsNullOrEmpty(videoUrl))
            {
                Metafield videoMeta = new Metafield();
                videoMeta.@namespace = "app";
                videoMeta.key = "video";
                videoMeta.value_type = "string";
                videoMeta.value = videoUrl;
                metafields.Add(videoMeta);
            }
            obj.metafields = metafields;

            //if (variantID.HasValue)
            //    variant.id = variantID.Value;

            if (includeVariants)
            {
                Variant variant = GetProductVariantObject(article, null, null, null, null, null, true, articleName);
                Variant variantVat = GetProductVariantObject(article, null, null, null, null, null, false, articleName);

                List<Variant> variants = new List<Variant> { variant, variantVat };
                obj.variants = variants;
            }


            // Serialize to Shopify JSON string
            string data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            data = "{ \"product\": " + data + "}";
            return data;
        }


        /// <summary>
        /// Converts Visma Customer object into Customer JSON accepted by SHopify.
        /// </summary>
        /// <param name="article">Visma Customer object</param>
        /// <returns>Customer JSON string</returns>
        public string GetCustomerDataFromVismaCustomer(Customer customer, Contact contact, Customer invoiceCustomer, long? addressID)
        {
            Customer addressCustomer = (invoiceCustomer == null) ? customer : invoiceCustomer;

            ShopifyCustomerSimple obj = new ShopifyCustomerSimple();

            string name = customer.Name1;
            string[] nameParts = name.Split(new char[] {' '}, 2);
            string firstName = nameParts[0];
            string lastName = (nameParts.Length > 1) ? nameParts[1] : "";
            string email = contact.Email;
            string tags = string.Format("+C{0}", customer.Number);

            if (customer.PricelistId > 0)
                tags += string.Format(", +P{0}", customer.PricelistId);

            obj.first_name = firstName;
            obj.last_name = lastName;
            obj.email = email;
            obj.tags = tags;

            //if (id.HasValue)
            //    obj.id = id.Value;
            

            ShopifyAddress[] addresses = new ShopifyAddress[1];

            ShopifyAddress adr = new ShopifyAddress();

            string[] contactNameParts = contact.Name.Split(new char[] { ' ' }, 2);
            string contactFirstName = contactNameParts[0];
            string contactLastName = (contactNameParts.Length > 1) ? contactNameParts[1] : "";

            string[] cityParts = addressCustomer.City.Split(new char[] { ' ' }, 2);
            string zip = cityParts[0];
            string city = (cityParts.Length > 1) ? cityParts[1] : "";
            long zipNumber;

            string phone = (!string.IsNullOrEmpty(contact.Phone)) ? contact.Phone : contact.MobilePhone;

            if (!long.TryParse(zip, out zipNumber))
            {
                zip = "";
                city = addressCustomer.City;
            }

            adr.first_name = contactFirstName;
            adr.last_name = contactLastName;
            // Save company name if different than conact name
            if (contact.Name != customer.Name1)
                adr.company = customer.Name1;
            adr.address1 = addressCustomer.StreetAddress;
            adr.city = city;
            adr.zip = zip;
            if (!string.IsNullOrEmpty(addressCustomer.Country))
            {
                if (addressCustomer.Country.Length == 2)
                {
                    adr.country_code = addressCustomer.Country;
                }
                else
                {
                    // Try to convert country name to country code
                    string countryName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(addressCustomer.Country.ToLower());
                    var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(x => new RegionInfo(x.LCID));
                    var englishRegion = regions.FirstOrDefault(region => region.EnglishName.Contains(countryName));
                    if (englishRegion != null)
                        adr.country_code = englishRegion.TwoLetterISORegionName;
                }
            }
            
            adr.phone = phone;
            adr.@default = true;

            if ((!string.IsNullOrEmpty(contactFirstName)) && (!string.IsNullOrEmpty(contactLastName))) {
                obj.first_name = contactFirstName;
                obj.last_name = contactLastName;
            }

            if (addressID.HasValue)
                adr.id = addressID.Value;

            addresses[0] = adr;
         

            obj.default_address = addresses[0];
            obj.addresses = addresses;
             

            // Serialize to Shopify JSON string
            string data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            data = "{ \"customer\": " + data + "}";
            return data;
        }

        /// <summary>
        /// Converts image file into product image JSON accepted by SHopify.
        /// </summary>
        /// <param name="article">Visma Article object</param>
        /// <returns>Product JSON string</returns>
        public string GetProductImageData(Article article, string imageData)
        {

            ShopifyProductImage obj = new ShopifyProductImage();

            obj.position = 1;
            obj.filename = string.Format("{0}.jpg", article.ArticleCode);
            obj.attachment = imageData;

            // Serialize to Shopify JSON string
            string data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            data = "{ \"image\": " + data + "}";
            return data;
        }

        public Variant GetProductVariantObject(Article article, long? variantID, int? pricelistNumber, int? customerNumber, int? quantity, decimal? price, bool taxable, string articleName)
        {
            string variantName = (string.IsNullOrEmpty(article.FamilyCode)) ? "Default" : article.ArticleName;

            if (!string.IsNullOrEmpty(articleName))
                variantName = variantName.StartsWith(articleName) ? variantName.Remove(0, articleName.Length).Trim() : variantName;

            if (pricelistNumber != null)
                variantName += string.Format(" [P:{0}]", pricelistNumber);

            if (customerNumber != null)
                variantName += string.Format(" [C:{0}]", customerNumber);

            if ((quantity != null) && (quantity > 1))
                variantName += string.Format(" [Q:{0}]", quantity);

            if (!taxable)
                variantName += " [V]";

            Variant variant = new Variant();
            variant.title = "";
            variant.option1 = variantName;
            variant.price = (price == null) ? this.ToPrice(article.Price1) : this.ToPrice(price.Value);
            variant.sku = article.ArticleCode;
            variant.barcode = article.EanCode;
            variant.grams = this.ToGrams(article.Weight);
            variant.taxable = taxable;
            //variant.position = 1;
            int inventoryQuantity = this.ToQuantity(article.DefaultWarehouse.Amount - article.DefaultWarehouse.OutwardAmount);
            if (inventoryQuantity > 0)
            {
                variant.inventory_management = "shopify";
                variant.inventory_quantity = inventoryQuantity;
            }


            if (variantID.HasValue)
                variant.id = variantID.Value;

            return variant;

        }

        public string GetProductVariantData(Article article, long? variantID, int? pricelistNumber, int? customerNumber, int? quantity, decimal? price, bool taxable, string articleName)
        {
            Variant variant = GetProductVariantObject(article, variantID, pricelistNumber, customerNumber, quantity, price, taxable, articleName);

            // Serialize to Shopify JSON string
            string data = JsonConvert.SerializeObject(variant, Formatting.Indented);
            data = "{ \"variant\": " + data + "}";

            return data;

            

        }

        public void CreateUpdateProduct(Article article, ref long? shopifyProductID, ref long? shopifyVariantID, ref long? shopifyVariantVatID, DateTime? deliveryDate, string articleName, decimal? vatRate, string videoUrl)
        {
            
            object response = null;
            long productID;
            long variantID;
            long variantVatID;

            if (!shopifyProductID.HasValue)
            {
                // Create new Product
                string productData = GetProductDataFromVismaArticle(article, deliveryDate, articleName, vatRate, videoUrl, true);
                response = CreateProduct(productData);

                JObject productResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                long.TryParse(productResponse["product"]["id"].ToString(), out productID);

                shopifyProductID = productID;
                long.TryParse(productResponse["product"]["variants"][0]["id"].ToString(), out variantID);
                long.TryParse(productResponse["product"]["variants"][1]["id"].ToString(), out variantVatID);
                shopifyVariantID = variantID;
                shopifyVariantVatID = variantVatID;
            }
            else
            {
                // Update existing Product
                string productData = GetProductDataFromVismaArticle(article, deliveryDate, articleName, vatRate, videoUrl, false);
                response = UpdateProduct(productData, shopifyProductID.Value);

                JObject productResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                long.TryParse(productResponse["product"]["id"].ToString(), out productID);

                shopifyProductID = productID;

                CreateUpdateProductVariant(article, shopifyProductID.Value, ref shopifyVariantID, null, null, null, null, true, articleName);
                CreateUpdateProductVariant(article, shopifyProductID.Value, ref shopifyVariantVatID, null, null, null, null, false, articleName);
            }
        }

        public void CreateUpdateProductVariant(Article article, long shopifyProductID, ref long? shopifyVariantID, int? pricelistNumber, int? customerNumber, int? quantity, decimal? price, bool taxable, string articleName)
        {
            string productVariantData = GetProductVariantData(article, shopifyVariantID, pricelistNumber, customerNumber, quantity, price, taxable, articleName);
            long variantID;

            if (!shopifyVariantID.HasValue)
            {
                object response = CreateProductVariant(shopifyProductID, productVariantData);

                JObject productVariantResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                long.TryParse(productVariantResponse["variant"]["id"].ToString(), out variantID);
                shopifyVariantID = variantID;
            }
            else
            {
                object response = UpdateProductVariant(productVariantData, shopifyVariantID.Value);

                JObject productVariantResponse = JsonConvert.DeserializeObject<JObject>(response.ToString());
                long.TryParse(productVariantResponse["variant"]["id"].ToString(), out variantID);
                shopifyVariantID = variantID;
            }
        }

        public void CreateUpdateProductVariants(Article article, long shopifyProductID, ref long? shopifyVariantID, ref long? shopifyVariantVatID, int? pricelistNumber, int? customerNumber, int? quantity, decimal? price, string articleName)
        {
            CreateUpdateProductVariant(article, shopifyProductID, ref shopifyVariantID, pricelistNumber, customerNumber, quantity, price, true, articleName);
            CreateUpdateProductVariant(article, shopifyProductID, ref shopifyVariantVatID, pricelistNumber, customerNumber, quantity, price, false, articleName);
        }


        public void LogShopifyToFileRequest(string method, string url, object data)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                string.Format("log_shopify_{0}.txt", DateTime.Now.ToString("yyyy_MM_dd")));
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1} {2}", System.DateTime.Now, method, url);
                sw.WriteLine(logLine);
                if (data != null)
                    sw.WriteLine(data.ToString());

            }
            finally
            {
                sw.Close();
            }
        }

        public void LogShopifyToFileResponse(object response)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                string.Format("log_shopify_{0}.txt", DateTime.Now.ToString("yyyy_MM_dd")));
            try
            {
                if (response != null)
                    sw.WriteLine(string.Format(">> {0}", response.ToString()));
                sw.WriteLine("");

            }
            finally
            {
                sw.Close();
            }
        }
        

    }
}
