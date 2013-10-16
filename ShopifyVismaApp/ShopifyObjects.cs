using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ShopifyVismaApp
{
    public class ShopifyProduct
    {
        public int key { get; set; }
        public string body_html { get; set; }
        public DateTime created_at { get; set; }
        public string handle { get; set; }
        public int id { get; set; }
        public string product_type { get; set; }
        public DateTime published_at { get; set; }
        public string template_suffix { get; set; }
        public string title { get; set; }
        public DateTime updated_at { get; set; }
        public string vendor { get; set; }
        public string tags { get; set; }
        public List<Variant> variants { get; set; }
        public List<Image> images { get; set; }
        public List<Options> options { get; set; }
    }

    public class ShopifyProductSimple
    {
  
        public string body_html { get; set; }
        public string product_type { get; set; }
        public string title { get; set; }
    }

    public class Variant
    {

        public int key { get; set; }
        public double? compare_at_price { get; set; }
        public DateTime created_at { get; set; }
        public string fulfillment_service { get; set; }
        public double grams { get; set; }
        public int id { get; set; }
        public string inventory_management { get; set; }
        public string inventory_policy { get; set; }
        public string option1 { get; set; }
        public string option2 { get; set; }
        public string option3 { get; set; }
        public int position { get; set; }
        public double? price { get; set; }
        public int product_id { get; set; }
        public bool requires_shipping { get; set; }
        public string sku { get; set; }
        public bool taxable { get; set; }
        public string title { get; set; }
        public DateTime updated_at { get; set; }
        public int inventory_quantity { get; set; }
    }

    public class Image
    {

        public int key { get; set; }
        public DateTime created_at { get; set; }
        public int id { get; set; }
        public int position { get; set; }
        public int product_id { get; set; }
        public DateTime updated_at { get; set; }
        public string src { get; set; }
    }

    public class Options
    {

        public int id { get; set; }
        public string name { get; set; }
    }
}

