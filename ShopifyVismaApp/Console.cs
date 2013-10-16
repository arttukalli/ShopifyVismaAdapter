using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Configuration;

using ShopifyAPIAdapterLibrary;

using NovaSDK;
using Nova.CustomerMgmtLibrary;
using Nova.WarehouseMgmtLibrary;

using Newtonsoft.Json;



namespace ShopifyVismaApp
{
    public partial class Console : Form
    {
        public Console()
        {
            InitializeComponent();
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            Log("Starting Integration Test.");

            // Initialize connections to Shopify and Visma
            Shopify shop = new Shopify();
            Visma visma = new Visma();

            // Get a list of Visma Articles
            ArticleList list = visma.GetArticleList();
            Log(string.Format("{0} products found in Visma database.", list.Count));

            int articlesCount = 0;
            int articleLimit = 3;
            foreach (ArticleInfo articleInfo in list)
            {
                string articleCode = articleInfo.ArticleCode;
                Log(string.Format(" - Processing Visma Article {0} (Code: {1})", articleInfo.ArticleName, articleInfo.ArticleCode));
                articlesCount += 1;

                // Get full Article object from Visma
                Article article = visma.GetArticleByCode(articleCode);
                
                // Save article as Shopify Product
                string productData = shop.GetProductDataFromVismaArticle(article);
                //Log(productData.ToString());

                object response = shop.CreateProduct(productData);
                Log(" - Shopify Product created.");
                //Log("Response " + response.ToString());

                if (articlesCount >= articleLimit)
                    break;
            }


        }

        /// <summary>
        /// Log to textarea.
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {
            OutputTextBox.Text += text + Environment.NewLine;
        }
    }


}
