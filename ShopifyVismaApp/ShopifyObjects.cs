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
        public long id { get; set; }
        public string product_type { get; set; }
        public bool published { get; set; }
        public DateTime published_at { get; set; }
        public string template_suffix { get; set; }
        public string title { get; set; }
        public DateTime updated_at { get; set; }
        public string vendor { get; set; }
        public string tags { get; set; }
        public List<Variant> variants { get; set; }
        public List<ShopifyProductImage> images { get; set; }
        public List<Options> options { get; set; }
        public List<Metafield> metafields { get; set; }
    }

    public class ShopifyProductSimple
    {
  
        //public string body_html { get; set; }
        public string product_type { get; set; }
        public bool published { get; set; }
        public string title { get; set; }
        public string tags { get; set; }
        public List<Variant> variants { get; set; }
        public List<Metafield> metafields { get; set; }
    }

    public class Variant
    {

        //public int key { get; set; }
        public string barcode { get; set; }
        //public double? compare_at_price { get; set; }
        //public DateTime created_at { get; set; }
        //public string fulfillment_service { get; set; }
        public double? grams { get; set; }
        public long? id { get; set; }
        public string inventory_management { get; set; }
        //public string inventory_policy { get; set; }
        public string option1 { get; set; }
        //public string option2 { get; set; }
        //public string option3 { get; set; }
        public int position { get; set; }
        public double? price { get; set; }
        //public int product_id { get; set; }
        //public bool requires_shipping { get; set; }
        public string sku { get; set; }
        public bool taxable { get; set; }
        public string title { get; set; }
        //public DateTime updated_at { get; set; }
        public int inventory_quantity { get; set; }
    }

    public class ShopifyProductImage
    {
        public string attachment { get; set; }
        //public int key { get; set; }
        //public DateTime created_at { get; set; }
        public string filename { get; set; }
        //public int id { get; set; }
        public int position { get; set; }
        //public int product_id { get; set; }
        //public DateTime updated_at { get; set; }
        //public string src { get; set; }
        public List<Metafield> metafields { get; set; }
    }

    public class Options
    {

        public int id { get; set; }
        public string name { get; set; }
    }

    public class Metafield
    {

        public string key { get; set; }
        public string @namespace { get; set; }
        public string value { get; set; }
        public string value_type { get; set; }
    }

    public class ShopifyCustomerSimple
    {
        //public long id { get; set; }
        public long id { get; set; }
        public ShopifyAddress default_address { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string tags { get; set; }
        public ShopifyAddress[] addresses { get; set; }
    }

    public class ShopifyAddress
    {

        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string company { get; set; }
        public string country_code { get; set; }
        public bool @default { get; set; }
        public string first_name { get; set; }
        public long? id { get; set; }
        public string last_name { get; set; }
        public string phone { get; set; }
        public string province_code { get; set; }
        public string zip { get; set; }
    }

    public class ShopifyLineItem
    {
        public long id { get; set; }
        public long? product_id { get; set; }
        public double? price { get; set; }
        public int quantity { get; set; }
        public string sku { get; set; }
        public string title { get; set; }
        public long? variant_id { get; set; }
    }

    public class ShopifyShippingLineItem
    {
        public string code { get; set; }
        public double? price { get; set; }
        public string title { get; set; }

    }

    public class ShopifyNoteAttributes
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ShopifyOrder
    {
        public DateTime? cancelled_at { get; set; }
        public DateTime created_at { get; set; }
        public string currency { get; set; }
        public long id { get; set; }
        public string email { get; set; }
        public string financial_status { get; set; }
        public string gateway { get; set; }
        public string name { get; set; }
        public long number { get; set; }
        public long order_number { get; set; }
        public double? subtotal_price { get; set; }
        public double? total_price { get; set; }
        public double? total_tax { get; set; }
        public DateTime updated_at { get; set; }
        public ShopifyAddress billing_address { get; set; }
        public ShopifyAddress shipping_address { get; set; }
        public ShopifyCustomerSimple customer { get; set; }
        public ShopifyLineItem[] line_items { get; set; }
        public ShopifyShippingLineItem[] shipping_lines { get; set; }
        public ShopifyNoteAttributes[] note_attributes { get; set; }

        public string GetNoteAttribute(string name)
        {
            var values = note_attributes.Where(x => x.name == name).Select(x => x.value);
            return values.Count() > 0 ? values.First() : null;
        }
    }
}

