using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.SalesOrderMgmtLibrary;
using Nova.PricelistMgmtLibrary;

namespace ShopifyVismaApp
{
    class Visma
    {

        public NovaContext _context;
        public int company;
        public int orderType = 15;

        public Visma(int company)
        {
            this.company = company;
            this._context = GetContext();
        }


        /// <summary>
        /// Get Context object to Nova Visma instance.
        /// </summary>
        /// <returns></returns>
        public NovaContext GetContext()
        {

            string vismaServer = ConfigurationSettings.AppSettings["Visma.Server"];
            int vismaCompany = this.company;
            NovaContext context = new NovaContext(vismaServer, vismaCompany);

            Nova.Configuration.Settings.SetDataConnectionString(context.CompanyConnectionString);


            return context;
        }

        public decimal ToPrice(double number)
        {
            decimal price;
            string numberString = number.ToString();
            decimal.TryParse(numberString, out price);

            return price;
        }

        public string GetImagePath()
        {
            string path = ConfigurationSettings.AppSettings["Visma.ImagesPath"];
            int vismaCompany = int.Parse(ConfigurationSettings.AppSettings["Visma.Company"]);

            return string.Format(path, vismaCompany);

        }

        public string GetImageFilePath(string code)
        {
            return string.Format("{0}{1}.jpg", this.GetImagePath(), code);
        }

        /// <summary>
        /// Get a list of all Articles.
        /// </summary>
        /// <returns></returns>
        public ArticleList GetArticleList(DateTime? startDate)
        {
            Nova.Common.NovaFieldManager fieldManager = new Nova.Common.NovaFieldManager();
            if (startDate.HasValue)
                return ArticleList.GetChangedProducts(_context, startDate.Value, fieldManager);
            else
                return ArticleList.GetArticles(this._context);
        }

        /// <summary>
        /// Get Article object by its code.
        /// </summary>
        /// <param name="articleCode"></param>
        /// <returns></returns>
        public Article GetArticleByCode(string articleCode)
        {
            return Article.Read(this._context, articleCode);
        }

        /// <summary>
        /// Get a list of all Customers.
        /// </summary>
        /// <returns></returns>
        public CustomerList GetCustomerList(DateTime? startDate)
        {
            List<string> criteria = new List<string>();
            if (startDate.HasValue)
                return CustomerList.GetChangedCustomers(this._context, startDate.Value);
            else
                return CustomerList.GetCustomersWithCriteria(this._context, criteria);
        }

        /// <summary>
        /// Get Customer object by its code.
        /// </summary>
        /// <param name="customerNumber"></param>
        /// <returns></returns>
        public Customer GetCustomerByNumber(int customerNumber)
        {
            return Customer.Read(this._context, customerNumber);
        }

        public GeneralPricelist GetGeneralPricelist(int iNumber)
        {
            return GeneralPricelist.Read(this._context, iNumber);
        }
    }


}
