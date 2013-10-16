using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.SalesOrderMgmtLibrary;

namespace ShopifyVismaApp
{
    class Visma
    {

        private NovaContext _context;

        public Visma() {
            this._context = GetContext();
        }


        /// <summary>
        /// Get Context object to Nova Visma instance.
        /// </summary>
        /// <returns></returns>
        public NovaContext GetContext()
        {
            string vismaServer = ConfigurationSettings.AppSettings["Visma.Server"];
            int vismaCompany = int.Parse(ConfigurationSettings.AppSettings["Visma.Company"]);
            NovaContext context = new NovaContext(vismaServer, vismaCompany);

            return context;
        }

        /// <summary>
        /// Get a list of all Articles.
        /// </summary>
        /// <returns></returns>
        public ArticleList GetArticleList() {
            return ArticleList.GetArticlesFromContext(this._context);
        }

        /// <summary>
        /// Get Article object by its code.
        /// </summary>
        /// <param name="articleCode"></param>
        /// <returns></returns>
        public Article GetArticleByCode(string articleCode)
        {
            return Article.ReadFromContext(this._context, articleCode);
        }

        /// <summary>
        /// Get a list of all Customers.
        /// </summary>
        /// <returns></returns>
        //public CustomerList GetCustomerList()
        //{
        //    return CustomerList.GetArticlesFromContext(this._context);
        //}
    }


}
