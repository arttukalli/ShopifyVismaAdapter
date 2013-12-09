using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using ShopifyAPIAdapterLibrary;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.PricelistMgmtLibrary;

using Newtonsoft.Json;

namespace ShopifyVismaApp
{
    public class Shopify
    {
        private ShopifyAPIClient _api;
        private string _token;
        public string account;
        public int ID;

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
        /// Get list of all Shopify products.
        /// </summary>
        /// <returns></returns>
        public object GetProducts()
        {
            return this._api.Get("/admin/products.json");
        }

        /// <summary>
        /// Get list of all Shopify customers.
        /// </summary>
        /// <returns></returns>
        public object GetCustomers()
        {
            return this._api.Get("/admin/customers.json");
        }

        /// <summary>
        /// Get list of all Shopify orders.
        /// </summary>
        /// <returns></returns>
        public object GetOrders()
        {
            return this._api.Get("/admin/orders.json");
        }

        /// <summary>
        /// Creates a new product in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateProduct(object data)
        {
            object response = this._api.Post("/admin/products.json", data);
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
            object response = this._api.Post(url, data);
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
            object response = this._api.Post(url, data);
            return response;
        }

        /// <summary>
        /// Creates a new customer in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateCustomer(object data)
        {
            object response = this._api.Post("/admin/customers.json", data);
            return response;
        }


        /// <summary>
        /// Get list of Orders from Shopify.
        /// </summary>
        /// <param name="changedDate">Set to retrieve Orders created or modified after this date.</param>
        /// <returns></returns>
        public object GetOrders(DateTime changedDate)
        {
            //var data = { };
            object response = this._api.Get("/admin/orders.json");
            return response;
        }

        public double? ToPrice(decimal number)
        {
            double price;
            string numberString = number.ToString();
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
        public string GetProductDataFromVismaArticle(Article article) {

            //string productData1 =
            //    "{" +
            //    "\"product\": {" +
            //    "\"title\": \"" + article.ArticleName + "\"," +
            //    "\"body_html\": \"\"," +
            //    "\"product_type\": \"Product\"" +
            //    "}" +
            //    "}";

            ShopifyProductSimple obj = new ShopifyProductSimple();
            obj.title = article.ArticleName;
            obj.body_html = "";
            obj.product_type = "Product";

            string tags = string.Format("+T{0}", article.ArticleType);
            if (!string.IsNullOrEmpty(article.MouldCode) && (article.MouldCode.Trim().ToUpper() == "TILAUSTUOTE"))
            {
                tags += ",Order";
            }

            if ((article.ProductGroupObject != null) && (!string.IsNullOrEmpty(article.ProductGroupObject.Description))) {
                tags += (string.IsNullOrEmpty(tags)) ? "" : ",";
                tags += "\"" + article.ProductGroupObject.Description + "\"";
            }

            

            obj.tags = tags;

            string variantName = (string.IsNullOrEmpty(article.FamilyCode)) ? "Default" : article.ArticleName;

            Variant variant = new Variant();
            variant.title = "";
            variant.option1 = variantName;
            variant.price = this.ToPrice(article.Price1);
            variant.sku = article.ArticleCode;
            variant.barcode = article.EanCode;
            variant.grams = this.ToGrams(article.Weight);
            variant.position = 1;
            int inventoryQuantity = this.ToQuantity(article.DefaultWarehouse.Amount);
            if (inventoryQuantity > 0)
            {
                variant.inventory_management = "shopify";
                variant.inventory_quantity = inventoryQuantity;
            }
            
            List<Variant> variants = new List<Variant> { variant };

            /*

            int GeneralPricelistMaxNumber = 44;
            for (int i = 0; i <= GeneralPricelistMaxNumber; i++)
            {
                try
                {
                    GeneralPricelist priceLlist = GeneralPricelist.Read(visma._context, i);

                    //Log("Pricelist " + i.ToString());
                    var pricelistItems = priceLlist.Where(x => x.ProductCode.Trim() == article.ArticleCode);
                    foreach (PricelistItem pricelistItem in pricelistItems)
                    {
                        Variant variantCustom = new Variant();
                        string title = string.Format("[G{0}]", i);
                        if (pricelistItem.BatchSize > 0)
                        {
                            title += string.Format("[Q{0}]", this.ToQuantity(pricelistItem.BatchSize));
                        }
                        variantCustom.title = title;
                        variantCustom.option1 = title;
                        variantCustom.price = this.ToPrice(pricelistItem.ContractPrice);
                        variantCustom.position = 2;
                        variantCustom.sku = article.ArticleCode;
                        variantCustom.barcode = article.EanCode;
                        variantCustom.grams = this.ToGrams(article.Weight);
                        if (quantity > 0)
                        {
                            variantCustom.inventory_management = "shopify";
                            variantCustom.inventory_quantity = quantity;
                        }

                        variants.Add(variantCustom);
                        //Log("PG " + i.ToString() + " " + pricelistItem.ContractPrice.ToString() + " " + pricelistItem.BatchSize.ToString());
                    }
                }
                catch (Csla.DataPortalException ex)
                {
                    //Log(ex.ToString());
                }


            }
             */



            obj.variants = variants;

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
        public string GetCustomertDataFromVismaCustomer(Customer customer)
        {

            ShopifyCustomerSimple obj = new ShopifyCustomerSimple();
            string name = customer.Name1;
            string[] nameParts = name.Split(new char[] {' '}, 2);
            string firstName = nameParts[0];
            string lastName = (nameParts.Length > 1) ? nameParts[1] : "";
            string email = customer.Contacts[0].Email;
            string tags = string.Format("+C{0}", customer.Number);

            if (customer.PricelistId > 0)
                tags += string.Format(", +P{0}", customer.PricelistId);

            obj.first_name = firstName;
            obj.last_name = lastName;
            obj.email = email;
            obj.tags = tags;



            ShopifyAddress[] addresses = new ShopifyAddress[customer.Contacts.Count];

            for (int i = 0; i < customer.Contacts.Count; i++)
            {
                Contact contact = customer.Contacts[i];
                ShopifyAddress adr = new ShopifyAddress();

                string[] contactNameParts = contact.Name.Split(new char[] { ' ' }, 2);
                string contactFirstName = contactNameParts[0];
                string contactLastName = (contactNameParts.Length > 1) ? contactNameParts[1] : "";

                string[] cityParts = customer.City.Split(new char[] { ' ' }, 2);
                string zip = cityParts[0];
                string city = (cityParts.Length > 1) ? cityParts[1] : "";
                long zipNumber;

                string phone = (!string.IsNullOrEmpty(contact.Phone)) ? contact.Phone : contact.MobilePhone;

                if (!long.TryParse(zip, out zipNumber))
                {
                    zip = "";
                    city = customer.City;
                }

                adr.first_name = contactFirstName;
                adr.last_name = contactLastName;
                adr.company = customer.Name1;
                adr.address1 = customer.StreetAddress;
                adr.city = city;
                adr.zip = zip;
                adr.country_code = customer.Country;
                adr.phone = phone;

                addresses[i] = adr;
            }

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

            //string productData1 =
            //    "{" +
            //    "\"product\": {" +
            //    "\"title\": \"" + article.ArticleName + "\"," +
            //    "\"body_html\": \"\"," +
            //    "\"product_type\": \"Product\"" +
            //    "}" +
            //    "}";

            ShopifyProductImage obj = new ShopifyProductImage();

            obj.position = 1;
            obj.filename = string.Format("{0}.jpg", article.ArticleCode);
            obj.attachment = imageData;

            // Serialize to Shopify JSON string
            string data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            data = "{ \"image\": " + data + "}";
            return data;
        }

        public string GetProductVariantData(Article article, int? pricelistNumber, int? customerNumber, int? quantity, decimal? price)
        {
            string variantName = (string.IsNullOrEmpty(article.FamilyCode)) ? "Default" : article.ArticleName;

            if (pricelistNumber != null)
                variantName += string.Format(" [P:{0}]", pricelistNumber);

            if (customerNumber != null)
                variantName += string.Format(" [C:{0}]", customerNumber);

            if ((quantity != null) && (quantity > 1))
                variantName += string.Format(" [Q:{0}]", quantity);

            Variant variant = new Variant();
            variant.title = "";
            variant.option1 = variantName;
            variant.price = (price == null) ? this.ToPrice(article.Price1) : this.ToPrice(price.Value);
            variant.sku = article.ArticleCode;
            variant.barcode = article.EanCode;
            variant.grams = this.ToGrams(article.Weight);
            //variant.position = 1;
            int inventoryQuantity = this.ToQuantity(article.DefaultWarehouse.Amount);
            if (inventoryQuantity > 0)
            {
                variant.inventory_management = "shopify";
                variant.inventory_quantity = inventoryQuantity;
            }

            // Serialize to Shopify JSON string
            string data = JsonConvert.SerializeObject(variant, Formatting.Indented);
            data = "{ \"variant\": " + data + "}";
            return data;
        }

    }
}
