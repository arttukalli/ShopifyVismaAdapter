using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;
using Nova.SalesOrderMgmtLibrary;
using Nova.PricelistMgmtLibrary;
using Nova.InvoiceMgmtLibrary;

namespace ShopifyVismaApp
{
    class Visma
    {

        public NovaContext _context;
        public int company;
        //public int orderType = 12;
        //public int pendingOrderType = 60;
        //public int termsOfDelivery = 1;
        //public int sellerId = 530;

        //Wrange FI = #16 kanta YR5
        //Fysioline FI = #12 kanta YR5
        //Fysioline SE = #10 kanta YR2
        //Fysioline EE = #11 kanta YR4

        SqlConnection vismaConnection;
        DataSetTableAdapters.OTRIVITableAdapter otrivitTA;
        DataSetTableAdapters.VARASTOTableAdapter varastoTA;
        DataSetTableAdapters.HINNASTOTableAdapter hinnastoTA;

        public Visma(int company)
        {
            this.company = company;
            this._context = GetContext();

            vismaConnection = GetSqlConnection();

            otrivitTA = new DataSetTableAdapters.OTRIVITableAdapter();
            otrivitTA.Connection = vismaConnection;

            varastoTA = new DataSetTableAdapters.VARASTOTableAdapter();
            varastoTA.Connection = vismaConnection;

            hinnastoTA = new DataSetTableAdapters.HINNASTOTableAdapter();
            hinnastoTA.Connection = vismaConnection;
        }


        /// <summary>
        /// Get Context object to Nova Visma instance.
        /// </summary>
        /// <returns></returns>
        public NovaContext GetContext()
        {

            string vismaServer = ConfigurationSettings.AppSettings["Visma.Server"];
            string vismaUsername = ConfigurationSettings.AppSettings["Visma.Server"];
            string vismaPassword = ConfigurationSettings.AppSettings["Visma.Password"];
            int vismaCompany = this.company;

            NovaContext context;
            
            if (string.IsNullOrEmpty(vismaUsername) || string.IsNullOrEmpty(vismaPassword))
                context = new NovaContext(vismaServer, vismaCompany);
            else
                context = new NovaContext(vismaServer, vismaUsername, vismaPassword, vismaCompany);

            Nova.Configuration.Settings.SetDataConnectionString(context.CompanyConnectionString);
            
            return context;
        }


        public SqlConnection GetSqlConnection()
        {
            SqlConnection sqlc = new SqlConnection(this._context.CompanyConnectionString);
            return sqlc;
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
            int vismaCompany = this.company;

            return string.Format(path, vismaCompany);

        }

        public string GetImageFilesFilter(string code)
        {
            return string.Format("{0}*.*", code);
        }

        public string GetVideoFilePath(string code)
        {
            return string.Format("{0}{1}.txt", this.GetImagePath(), code);
        }


        public string GetCollectionsPath()
        {
            string filename = ConfigurationSettings.AppSettings["Visma.CollectionsFile"];

            return this.GetImagePath() + filename;

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

        /// <summary>
        /// Get delivery date for product from Visma OTRIVIT or VARASTO tables.
        /// </summary>
        /// <param name="articleCode">Visma article code</param>
        /// <returns></returns>
        public DateTime? GetDeliveryDate(string articleCode)
        {
            DateTime? date = null;
            DataSet.OTRIVIDataTable otrivitDT = otrivitTA.GetDataByArticleCode(articleCode);

            if (otrivitDT.Count > 0)
            {
                date = otrivitDT[0].TALKUPVM;
            }
            
            if (date == null)
            {
                DataSet.VARASTODataTable varastoDT = varastoTA.GetDataByArticleCode(articleCode);

                if (varastoDT.Count > 0)
                {
                    int days = -1;
                    string daysString = varastoDT[0].TOIMAIKA;
                    int.TryParse(daysString, out days);

                    if (days >= 0)
                    {
                        date = DateTime.Now.AddDays(days);
                    }
                }
            }

            return date;
        }

        /// <summary>
        /// Get VAT Rate for given Article.
        /// </summary>
        /// <param name="articleCode">Article code</param>
        /// <returns></returns>
        public decimal? GetVatRate(string articleCode)
        {
            decimal? vatRate = null;

            vatRate = varastoTA.GetVATByArticleCode(articleCode);
  
            return vatRate;
        }

        /// <summary>
        /// Get points for given Article.
        /// </summary>
        /// <param name="articleCode">Article code</param>
        /// <returns></returns>
        public decimal? GetPoints(string articleCode)
        {
            decimal? points = null;

            points = varastoTA.GetPointsByArticleCode(articleCode);

            return points;
        }

        /// <summary>
        /// Get common part of multiple Article names with same FamilyCode.
        /// </summary>
        /// <param name="familyCode">Family code</param>
        /// <returns></returns>
        public string GetCommonArticleNameForFamilyCode(string familyCode)
        {
            string articleName = null;
            DataSet.VARASTODataTable varastotDT = varastoTA.GetDataByFamilyCode(familyCode);
            
            //string[] varastoNames = new string[] {  };
            List<string> varastoNames = new List<string>();
  
            if (varastotDT.Count > 0)
            {
                foreach (DataSet.VARASTORow varasto in varastotDT)
                {
                    string varastoName = varasto.NIMIKE;
                    varastoNames.Add(varastoName);
                }

                articleName = GetCommonString(varastoNames);
                
            }

            return articleName;
            
        }

        /// <summary>
        /// Get common part from list of strings.
        /// </summary>
        /// <param name="strings">List of strings</param>
        /// <returns></returns>
        public string GetCommonString(IEnumerable<string> strings)
        {
            var commonPrefix = strings.FirstOrDefault() ?? "";

            foreach (var s in strings)
            {
                var potentialMatchLength = Math.Min(s.Length, commonPrefix.Length);

                if (potentialMatchLength < commonPrefix.Length)
                    commonPrefix = commonPrefix.Substring(0, potentialMatchLength);

                for (var i = 0; i < potentialMatchLength; i++)
                {
                    if (s[i] != commonPrefix[i])
                    {
                        commonPrefix = commonPrefix.Substring(0, i);
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(commonPrefix))
                commonPrefix = commonPrefix.Trim();

            return commonPrefix;
        }


    }


}
