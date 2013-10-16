using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using ShopifyAPIAdapterLibrary;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;

using Newtonsoft.Json;

namespace ShopifyVismaApp
{
    public class Shopify
    {
        private ShopifyAPIClient _api;

        /// <summary>
        /// Initialize Shopify by creating a Shopify API Client object.
        /// </summary>
        public Shopify()
        {
            this._api = GetAPIClient();
        }

        /// <summary>
        /// Get API Client object with connection to a Shopify store.
        /// </summary>
        /// <returns></returns>
        public ShopifyAPIClient GetAPIClient()
        {
            
            ShopifyAuthorizationState authState = new ShopifyAuthorizationState();
            authState.ShopName = ConfigurationSettings.AppSettings["Shopify.StoreAccount"];
            authState.AccessToken = ConfigurationSettings.AppSettings["Shopify.AccessToken"];

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
        /// Creates a new customer in Shopify.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateCustomer(object data)
        {
            object response = this._api.Post("/admin/customer.json", data);
            return response;
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

            ShopifyProductSimple product = new ShopifyProductSimple();
            product.title = article.ArticleName;
            product.body_html = "";
            product.product_type = "Product";

            // Serialize to Shopify JSON string
            string productData = JsonConvert.SerializeObject(product, Formatting.Indented);
            productData = "{ \"product\": " + productData + "}";
            return productData;
        }

    }
}
