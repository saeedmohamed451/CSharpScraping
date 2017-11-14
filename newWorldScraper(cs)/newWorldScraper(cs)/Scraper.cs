using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System.Web;
using iTextSharp;
using System.IO;
using Excel;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace newWorldScraper_cs_
{
    public class Scraper
    {
        private HttpClient client = null;
        FileWriter writer = null;
        private string fileName = null;
        private WebClient web = null;
        private string started_date = string.Empty;
        private string ended_date = string.Empty;
        private string str_path = string.Empty;
        private string file_path = string.Empty;
        private string pdf_falePath = string.Empty;
        List<string> retailers = new List<string>();
        List<string> app_list = new List<string>();
        List<string> subsurb_list = new List<string>();
        List<string> name_list = new List<string>();
        List<string> Url_list = new List<string>();
        List<string> mainUrl_list = new List<string>();
        List<string> cat_ids = new List<string>();
        private string date_end = string.Empty;
        private string store_id = string.Empty;
        private string store_name = string.Empty;
        List<string> imageUrls = new List<string>();
        private string startDate = string.Empty;
        private string endDate = string.Empty;
        private string categoryId = string.Empty;
        private string title = string.Empty;
        private string pageNum = string.Empty;
        private string ppname = string.Empty;
        private List<string> category_ids = new List<string>();
        private List<string> pageList = new List<string>();
        private List<string> start_list = new List<string>();
        private List<string> end_list = new List<string>();
        private List<string> pdf_list = new List<string>();

        private void initHttpClient(CookieContainer container)
        {
            HttpClientHandler handler = new HttpClientHandler();

            container = new CookieContainer();
            handler.CookieContainer = container;
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        }

        //get store_id
        private void get_storeId(string content, string post_code, string referUrl)
        {
            try
            {
                string storeUrl = string.Empty;
                //log "1"
                string text = post_code.Split('-')[1].Trim();
                //log "2"
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                //log "3"
                doc.LoadHtml(content);
                IEnumerable<HtmlNode> urldivs = doc.DocumentNode.Descendants("a").Where(node => node.Attributes["href"].Value.Contains("?store") && node.InnerText.Contains(text));
                storeUrl = urldivs.ToArray()[0].Attributes["href"].Value;

                client.DefaultRequestHeaders.Referrer = null;
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", referUrl);
                string location_url = referUrl + storeUrl;
                Uri uri = new Uri(location_url);
                var result = client.GetAsync(uri).Result;
                result.EnsureSuccessStatusCode();
                string str2 = result.Content.ReadAsStringAsync().Result;
                HtmlAgilityPack.HtmlDocument odc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(str2);
                HtmlNode node1 = doc.DocumentNode.SelectSingleNode(".//a[@class='pdf']");
                pdf_falePath = "http://www.newworld.co.nz" + node1.Attributes["href"].Value;
                string virtualUrl = "http://www.newworld.co.nz/savings/virtualmailer";
                var resultContent = client.GetAsync(virtualUrl).Result;
                resultContent.EnsureSuccessStatusCode();
                string str3 = resultContent.Content.ReadAsStringAsync().Result;

                Regex reg1 = new Regex("RenderMailer.*, *(\\d*)\\)");
                if (reg1.Match(str3).Success)
                {
                    store_id = reg1.Match(str3).Groups[1].ToString();
                }

            }
            catch (Exception ex)
            {

            }


        }

        //get_locationID
        private void getPageNum()
        {
            try
            {
                mainUrl_list.Clear();
                cat_ids.Clear();

                string referUrl = "http://app.redpepperdigital.net/app/redpepper/home/" + store_id + "?toolbar=no";
                string url = "http://app.redpepperdigital.net/client/" + store_id + "/catalogues/json";

                //client.DefaultRequestHeaders.Referrer = null;
                //client.DefaultRequestHeaders.Host = "app.redpepperdigital.net";
                client.DefaultRequestHeaders.Referrer = new Uri(referUrl);
                var resultContent = client.GetAsync(url).Result;
                resultContent.EnsureSuccessStatusCode();
                string content = resultContent.Content.ReadAsStringAsync().Result;
                dynamic jsonConetent = JsonConvert.DeserializeObject(content);
                foreach (dynamic json1 in jsonConetent)
                {
                    title = json1.share;

                    string categoryUrl = "http://app.redpepperdigital.net/catalogue/details/name/" + title + "/json";
                    var result1 = client.GetAsync(categoryUrl).Result;
                    result1.EnsureSuccessStatusCode();
                    string str4 = result1.Content.ReadAsStringAsync().Result;
                    str4 = Between(str4 + "xx", "[", "]xx");

                    //string str4 = Resource1.String1;

                    log("parsing : 1");
                    dynamic jsonResult = JsonConvert.DeserializeObject(str4);
                    log("parsing : 2");
                    pageNum = jsonResult.pages;
                    pageList.Add(pageNum);
                    log("parsing : 3");
                    pdf_list.Add("http://app.redpepperdigital.net" + jsonResult.pdf.Value);
                    DateTime startdate = jsonResult.start.Value;
                    log("parsing : 4 : " + startdate.ToString());
                    startdate = startdate.AddHours(14);
                    startDate = startdate.ToString();
                    startDate = startDate.Split(' ')[0];
                    start_list.Add(startDate);
                    log("parsing : 6");
                    DateTime enddate = jsonResult.finish.Value;
                    log("parsing : 7 : " + enddate.ToString());
                    endDate = enddate.ToString();
                    endDate = endDate.Split(' ')[0];
                    end_list.Add(endDate);
                    log("parsing : 9");
                    categoryId = jsonResult.id;
                    category_ids.Add(categoryId);
                    log("parsing : " + categoryId);

                }
            }
            catch (Exception e)
            {
                log("parsing : Exception : " + e.ToString());
            }


        }
        private string regStr(string content, string pattern, string replaceStr)
        {
            string result = string.Empty;
            Regex rgx = new Regex(pattern);
            result = rgx.Replace(content, replaceStr);

            return result;

        }

        private void log(string output)
        {
            FileWriter writer3 = new FileWriter("log.txt");
            writer3.WriteRow(output);
        }

        //Main scraping
        public bool newWorldScrap(string str, string root_dir, string storeUrl, string postcode, string file_name)
        {
            CookieContainer container = null;
            log("1");
            initHttpClient(container);
            log("2");
            string first_row = "CATALOGUE_START" + ',' + "CATALOGUE_END" + ',' + "RETAILER" + ',' + "Description" + ',' + "Page" + ',' + "Promo Type" + ',' + "Multibuy Qty" + ',' + "Price Per Unit" + ',' + "Price Discount";
            log("3");
            ppname = "started";
            log("4");
            var str_array = new List<string>();
            log("5");
            var page_array = new List<string>();
            string retailer = string.Empty;
            startDate = "";
            endDate = "";
            try
            {
                List<string> url_list = new List<string>();
                var firstResult = client.GetAsync(storeUrl).Result;
                log("6");
                firstResult.EnsureSuccessStatusCode();
                log("7");
                string firstContent = firstResult.Content.ReadAsStringAsync().Result;
                log("8");
                //get real scraping url
                retailer = str;
                get_storeId(firstContent, postcode, storeUrl);
                log("9");
                getPageNum();
                log("10");
                for (int k = 0; k < category_ids.Count; k++)
                {
                    if (pageNum == null)
                    {
                        return false;
                    }
                    int pageCnt = Int32.Parse(pageList[k]);
                    log("12");
                    string product_date = string.Empty;
                    string product_start = string.Empty;
                    string product_end = string.Empty;
                    imageUrls.Clear();

                    string[] strArray3 = null;
                    string[] strArray4 = null;
                    if (startDate.Contains('/'))
                    {
                        strArray3 = start_list[k].Trim().Split('/');
                        strArray4 = end_list[k].Trim().Split('/');
                    }
                    else if (startDate.Contains('-'))
                    {
                        strArray3 = start_list[k].Trim().Split('-');
                        strArray4 = end_list[k].Trim().Split('-');
                    }


                    // declare month, day varaiable
                    string m = strArray3[1];
                    string d = strArray3[0];
                    string m1 = strArray4[1];
                    string d1 = strArray4[0];

                    if (m.Length == 1)
                    {
                        m = "0" + m;
                    }
                    if (d.Length == 1)
                    {
                        d = "0" + d;
                    }
                    if (m1.Length == 1)
                    {
                        m1 = "0" + m1;
                    }
                    if (d1.Length == 1)
                    {
                        d1 = "0" + d1;
                    }

                    product_start = m + "/" + d + "/" + strArray3[2];
                    log("58");
                    product_end = m1 + "/" + d1 + "/" + strArray4[2];
                    log("59");
                    string fileDate = strArray3[2] + "_" + m + "_" + d;
                    log("60");
                    FileWriter writer2 = new FileWriter("error.txt");
                    file_path = "";

                    for (int i = 1; i < pageCnt + 1; i++)
                    {
                        try
                        {
                            string item_Url = "http://app.redpepperdigital.net/catalogue/" + category_ids[k] + "/page/" + i.ToString() + "/regions/json";
                            string refer_url = "http://app.redpepperdigital.net/app/redpepper/view/" + title + "?toolbar=no";
                            log("13");
                            client.DefaultRequestHeaders.Referrer = null;
                            client.DefaultRequestHeaders.Referrer = new Uri(refer_url);
                            log("14");
                            var mainResult = client.GetAsync(item_Url).Result;
                            log("15");
                            mainResult.EnsureSuccessStatusCode();
                            log("16");
                            string mainstr = mainResult.Content.ReadAsStringAsync().Result;
                            log("17");
                            Newtonsoft.Json.Linq.JArray content = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(mainstr);
                            log("18");
                            for (int j = 0; j < content.Count; j++)
                            {
                                log("19");
                                if (content[j]["coords"] != null)
                                {
                                    log("20");
                                    string coords = content[j]["coords"].ToString();
                                    log("21");
                                    string[] co_arr = coords.Split(',');
                                    log("22");
                                    content[j]["coords"] = int.Parse(co_arr[2].Substring((co_arr[2].Length - 2), 2));
                                    log("23");
                                }
                            }
                            Newtonsoft.Json.Linq.JArray sorted = new Newtonsoft.Json.Linq.JArray(content.OrderBy(obj => obj["coords"]));
                            log("24");
                            dynamic content1 = JsonConvert.DeserializeObject(sorted.ToString(Formatting.Indented));
                            log("25");

                            foreach (dynamic cat_item in content1)
                            {
                                log("28");

                                if (cat_item.field_product_image_url.Count == 0 && cat_item.field_product_title == "" && content1.Count == 1)
                                {
                                    string row1 = product_start + ',' + product_end + ',' + retailer + ',' + "BLANK" + ',' + i.ToString() + ',' + "" + ',' + "" + ',' + "" + ',' + "";
                                    writer.WriteRow(row1);
                                    continue;
                                }

                                if (File.Exists(root_dir + "//" + file_path))
                                {
                                    log("29");
                                    using (var stream = new StreamReader(root_dir + "//" + file_path))
                                    {
                                        log("30");
                                        while (!stream.EndOfStream)
                                        {
                                            log("31");
                                            var splits = stream.ReadLine().Split(',');
                                            log("32");
                                            str_array.Add(splits[3]);
                                            log("33");
                                            page_array.Add(splits[4]);
                                            log("34");

                                        }

                                    }

                                }

                                string item_name = string.Empty;
                                string itemUrl = string.Empty;
                                string page = string.Empty;
                                string product_des = string.Empty;
                                string product_disprice = string.Empty;
                                string product_regpirce = string.Empty;
                                string promo_type = string.Empty;
                                string image_url = string.Empty;
                                string multibuy_Qty = string.Empty;
                                float qty = 0;
                                if (cat_item.field_product_title == "")
                                {
                                    continue;
                                }
                                item_name = cat_item.field_product_title;
                                log("36");
                                itemUrl = cat_item.field_product_image_url[0];
                                log("37");
                                itemUrl = itemUrl.Replace("https", "http");
                                itemUrl = itemUrl.Replace(":443", "");
                                page = i.ToString();
                                log("38");

                                item_name = filterDescription(item_name);
                                if (item_name.Contains(",")) 
                                {
                                    item_name = "\"" + item_name + "\"";
                                }
                                ppname = item_name;
                                log("51");
                                item_name = item_name.Trim();
                                log("52");
                                product_des = item_name;
                                log("56");
                                if (product_des.Contains("NEW"))
                                {
                                    log("57");
                                    product_des = product_des.Replace("NEW", "").TrimStart();
                                    promo_type = "New Line";
                                }



                                //get product informations

                                //

                                file_name = file_name.Replace("_YYYY_MM_DD", "");
                                log("63");
                                if (k == 0)
                                {
                                    file_path = file_name + "_" + fileDate + ".csv";
                                    createDrectory(root_dir + "\\" + file_name + "_" + fileDate);
                                    log("64");
                                    downloadFile(root_dir + "\\" + file_name + "_" + fileDate, product_des, itemUrl);
                                    log("65");
                                    downloadPdf(pdf_list[k], root_dir + "//" + file_name + "_" + fileDate + ".pdf");
                                    log("66");
                                    writer = new FileWriter(root_dir + "//" + file_path);
                                    log("67");
                                    writer.WriteData(first_row, root_dir + "//" + file_path);
                                }
                                else
                                {
                                    if (start_list[k] == start_list[k - 1])
                                    {
                                        file_path = file_name + "_" + fileDate + "-" + "1" + ".csv";
                                        createDrectory(root_dir + "\\" + file_name + "_" + fileDate + "-" + "1");
                                        downloadFile(root_dir + "\\" + file_name + "_" + fileDate + "-" + "1", product_des, itemUrl);
                                        downloadPdf(pdf_list[k], root_dir + "//" + file_name + "_" + fileDate + "-" + "1" + ".pdf");
                                        writer = new FileWriter(root_dir + "//" + file_path);
                                        writer.WriteData(first_row, root_dir + "//" + file_path);

                                    }
                                    else
                                    {
                                        createDrectory(root_dir + "\\" + file_name + "_" + fileDate);
                                        file_path = file_name + "_" + fileDate + ".csv";
                                        downloadFile(root_dir + "\\" + file_name + "_" + fileDate, product_des, itemUrl);
                                        downloadPdf(pdf_list[k], root_dir + "//" + file_name + "_" + fileDate + ".pdf");
                                        writer = new FileWriter(root_dir + "//" + file_path);
                                        writer.WriteData(first_row, root_dir + "//" + file_path);
                                    }
                                }

                                log("68");
                                if (cat_item.field_product_discount_price == null)
                                {
                                    log("69");
                                }
                                else
                                {
                                    product_disprice = cat_item.field_product_discount_price;
                                    log("70");
                                }

                                if (cat_item.field_product_price == null)
                                {
                                    log("71");
                                }
                                else
                                {
                                    product_regpirce = cat_item.field_product_price;
                                    log("72");
                                    if (product_regpirce.Contains(""))
                                    {
                                        log("73");
                                        if (product_regpirce.Contains("for") && product_regpirce.Contains("ea"))
                                        {

                                        }
                                        if (product_regpirce.Contains("/"))
                                        {
                                            product_regpirce = Between(product_regpirce, "$", "/");
                                            log("74");
                                        }
                                        if (product_regpirce.Contains("for"))
                                        {
                                            product_regpirce = Between(product_regpirce + "xx", "for", "xx");
                                            log("75");

                                        }
                                        if (product_regpirce.Contains("or"))
                                        {
                                            product_regpirce = Between(product_regpirce, "$", "or");
                                            log("76");

                                        }
                                        product_regpirce = product_regpirce.Replace("100g", "");
                                        product_regpirce = product_regpirce.Replace("pk", "");
                                        product_regpirce = product_regpirce.Replace("kg", "");
                                        product_regpirce = product_regpirce.Replace("Save $", "");
                                        product_regpirce = product_regpirce.Replace("$", "");
                                        product_regpirce = product_regpirce.Replace("OFF RRP", "");
                                        product_regpirce = product_regpirce.Replace("off RRP", "");
                                        product_regpirce = product_regpirce.Replace("Off RRP", "");
                                        product_regpirce = product_regpirce.Replace("EA", "");
                                        product_regpirce = product_regpirce.Replace("SAVE $", "");
                                        product_regpirce = product_regpirce.Replace("Save up to $", "");
                                        product_regpirce = product_regpirce.Replace("Save UP TO $", "");
                                        product_regpirce = product_regpirce.Replace("SAVE UP TO $", "");
                                        product_regpirce = product_regpirce.Replace("Save Up To $", "");
                                        product_regpirce = product_regpirce.Replace("See In-Store", "");
                                        product_regpirce = product_regpirce.Replace("Save 50%", "");
                                        product_regpirce = product_regpirce.Replace("ea", "");
                                        product_regpirce = product_regpirce.Replace("each", "");
                                        product_regpirce = product_regpirce.Replace("EACH", "");
                                        product_regpirce = product_regpirce.Replace("¢", "");
                                        product_regpirce = product_regpirce.Replace("c", "");
                                        if (product_regpirce.Contains("%"))
                                        {
                                            log("77");
                                            product_regpirce = product_regpirce.Replace(product_regpirce, "");
                                        }
                                        product_regpirce = product_regpirce.Trim();
                                        log("78");


                                    }
                                }
                                string multi = string.Empty;
                                if (cat_item.field_product_price == null)
                                {
                                    log("79");
                                }
                                else
                                {

                                    multi = cat_item.field_product_price;
                                    log("80");
                                    multi = multi.ToLower();
                                    log("81");

                                }
                                if (multi.Contains("buy"))
                                {
                                    log("82");
                                }
                                if (multi.Contains("any") && multi.Contains("for"))
                                {
                                    multi = convertNum(multi);
                                    log("83");
                                    multibuy_Qty = Between(multi, "any", "for").Trim();
                                    log("84");
                                    qty = float.Parse(multibuy_Qty);
                                    log("85");
                                    if (product_disprice == "")
                                    {

                                    }
                                    else
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        log("86");

                                    }
                                    if (product_regpirce == "")
                                    {

                                    }
                                    else
                                    {
                                        product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                        log("87");
                                    }
                                }
                                else if (multi.Contains("all") && multi.Contains("for"))
                                {
                                    multi = convertNum(multi);
                                    log("88");
                                    multibuy_Qty = Between(multi, "all", "for").Trim();
                                    log("89");
                                    qty = float.Parse(multibuy_Qty);
                                    log("90");
                                    if (product_disprice == "")
                                    {
                                        log("91");
                                    }
                                    else
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        log("92");
                                    }
                                    if (product_regpirce == "")
                                    {
                                        log("93");
                                    }
                                    else
                                    {
                                        product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                        log("94");
                                    }

                                }
                                else if (multi.Contains("both for"))
                                {
                                    log("94");
                                }
                                else if (multi.Contains("buy") && multi.Contains("for"))
                                {
                                    multi = convertNum(multi);
                                    log("95");
                                    multibuy_Qty = Between(multi, "buy", "for").Trim();
                                    log("96");
                                    qty = float.Parse(multibuy_Qty);
                                    log("97");
                                    if (product_disprice == "")
                                    {
                                        log("98");
                                    }
                                    else if ((float.Parse(product_disprice) / qty).ToString("N2") == "0.50")
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        log("99");
                                        product_regpirce = "0";

                                    }
                                    else
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        log("100");
                                        if (product_regpirce != "")
                                        {
                                            product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                            log("101");

                                        }

                                    }
                                }
                                else if (multi.Contains("for"))
                                {

                                    multi = convertNum(multi);
                                    log("101");
                                    multibuy_Qty = Between("any" + multi, "any", "for").Trim();
                                    log("102");
                                    qty = float.Parse(multibuy_Qty);
                                    log("103");
                                    //product_disprice = Between("/" + product_disprice, "/", ".");
                                    if (product_disprice != "")
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        log("104");
                                        product_regpirce = "0";
                                    }

                                    else
                                    {
                                        log("105");
                                        if (product_regpirce != "")
                                        {
                                            product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                            log("106");

                                        }

                                    }

                                }

                                else if (multi.Contains("each"))
                                {
                                    log("107");
                                }

                                float dis_price = 0;
                                float reg_price = 0;

                                if (product_regpirce == string.Empty)
                                {
                                    reg_price = 0;
                                    log("108");
                                }
                                else
                                {
                                    reg_price = float.Parse(product_regpirce);
                                    log("109");

                                }
                                if (product_disprice == string.Empty)
                                {
                                    dis_price = 0;
                                    log("110");
                                }
                                else
                                {
                                    dis_price = float.Parse(product_disprice);
                                    log("111");

                                }

                                if (qty > 1)
                                {
                                    promo_type = "Multibuy";
                                    log("112");
                                }
                                else if (reg_price >= dis_price && dis_price > 0)
                                {
                                    if (promo_type != "New Line")
                                    {
                                        promo_type = "Half Price";
                                        log("113");

                                    }

                                }
                                else
                                {
                                    if (promo_type != "New Line")
                                    {
                                        promo_type = "Price Reduction";
                                        log("114");
                                    }
                                }
                                if (cat_item.field_product_description == null)
                                {
                                }
                                else
                                {
                                    string des = cat_item.field_product_description;
                                    log("115");
                                    des = des = des.ToLower();
                                    log("116");
                                    if (des.Contains("every day"))
                                    {
                                        promo_type = "EDLP";
                                        log("117");

                                    }
                                    else if (des.Contains("prices dropped"))
                                    {
                                        promo_type = "Price Drop";
                                        log("118");

                                    }
                                    else if (des.Contains("% off"))
                                    {
                                        promo_type = "% Off";
                                        product_disprice = "0";
                                        product_regpirce = "0";
                                        log("119");

                                    }



                                }
                                if (product_disprice == "") 
                                {
                                    product_disprice = "0";
                                }
                                if (product_regpirce == "") 
                                {
                                    product_regpirce = "0";
                                }
                             
                                    string row = product_start + ',' + product_end + ',' + retailer + ',' + product_des + ',' + page + ',' + promo_type + ',' + multibuy_Qty + ',' + product_regpirce + ',' + product_disprice;
                                    log("121");
                                    if (str_array.Count == 1)
                                    {
                                        writer.WriteRow(row);
                                        log("122");

                                    }
                                    else
                                    {
                                        if (!str_array.Contains(product_des))
                                        {
                                            writer.WriteRow(row);
                                            log("123");
                                        }
                                        else
                                        {
                                            int key = str_array.IndexOf(product_des);
                                            log("124");
                                            if (page_array[key] == page)
                                            {
                                                log("125");
                                            }
                                            else
                                            {
                                                writer.WriteRow(row);
                                                log("126");
                                            }

                                        }

                                    }

                                }

                        }
                        catch (Exception ex1)
                        {
                            log("127");
                            writer2.WriteData(ppname, "error.txt");

                        }


                    }

                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;




        }


        private string filterDescription(string originItemName)
        {
            string filteredItemName = "";

            filteredItemName = originItemName.Replace(" varieties", "");
            filteredItemName = originItemName.Replace(" Varieties", "");
            filteredItemName = originItemName.Replace("‘", "'");
            filteredItemName = originItemName.Replace("'", "'");
            filteredItemName = filteredItemName.Replace(" range", "");
            filteredItemName = filteredItemName.Replace("¥", "");
            filteredItemName = filteredItemName.Replace("®", "");
            filteredItemName = filteredItemName.Replace("ì", "");
            filteredItemName = filteredItemName.Replace("†", "");
            filteredItemName = filteredItemName.Replace("®", "");
            filteredItemName = filteredItemName.Replace("«", "");
            filteredItemName = filteredItemName.Replace("‡", "");
            filteredItemName = filteredItemName.Replace("^", "");
            filteredItemName = filteredItemName.Replace("~*", "");
            filteredItemName = filteredItemName.Replace("°", "");
            filteredItemName = filteredItemName.Replace("™", "");
            filteredItemName = filteredItemName.Replace("ø", "");
            filteredItemName = filteredItemName.Replace("’", "'");
            filteredItemName = filteredItemName.Replace("é", "e");
            filteredItemName = filteredItemName.Replace("É", "E");
            filteredItemName = filteredItemName.Replace("¥", "");
            filteredItemName = filteredItemName.Replace("ˆ", "");
            filteredItemName = filteredItemName.Replace("è", "e");
            filteredItemName = filteredItemName.Replace("±", "");
            filteredItemName = filteredItemName.Replace("#", "");
            filteredItemName = filteredItemName.Replace("~~", "");
            filteredItemName = filteredItemName.Replace("ô", "o");
            filteredItemName = filteredItemName.Replace("¢", "c");
            filteredItemName = filteredItemName.Replace(" selected", "");
            filteredItemName = filteredItemName.Replace(" ranges", "");
            filteredItemName = filteredItemName.Replace(" varieites", "");
            filteredItemName = filteredItemName.Replace(" varieities", "");
            filteredItemName = filteredItemName.Replace(" Varietes", "");
            filteredItemName = filteredItemName.Replace(" Asorted", "");
            filteredItemName = filteredItemName.Replace("Two for $", "2 for $");
            filteredItemName = filteredItemName.Replace("Three for $", "3 for $");
            filteredItemName = filteredItemName.Replace("Four for $", "4 for $");

            return filteredItemName;
        }

        //Create root directory
        public string createRootDirectory(string path, string scriptName)
        {
            if (path == null || path == "")
                path = Directory.GetCurrentDirectory();

            //string path = Directory.GetCurrentDirectory();
            path = path + "\\" + scriptName;
            bool exists = System.IO.Directory.Exists(path);

            if (!exists)
                Directory.CreateDirectory(path);
            return path;
        }

        //Create Sub directory
        private void createDrectory(string name)
        {

            bool exists = System.IO.Directory.Exists(name);
            log("130");

            if (!exists)
                Directory.CreateDirectory(name);
        }
      
        private string convertNum(string multi)
        {
            string str = multi.Replace("two", "2");
            log("133");
            str = str.Replace("three", "3");
            str = str.Replace("four", "4");
            str = str.Replace("five", "5");
            str = str.Replace("six", "6");
            log("134");
            return str;

        }
        //Download Image file
        private void downloadFile(string dir, string img_name, string link)
        {
            try
            {
                img_name = img_name.Replace("\"", "");
                img_name = img_name.Replace("/", "");

                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                log("135");
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    log("136");
                    return;
                }
                byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                log("137");
                if (contents == null || contents.Length < 1)
                    return;
                string imgName = dir + "\\" + img_name + ".jpg";
                log("138");
                System.IO.File.WriteAllBytes(imgName, contents);
                log("139");
            }
            catch (Exception e)
            {
            }
        }
        private bool downloadImage(string dir, string img_name, string link)
        {
            try
            {

                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
                byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                if (contents == null || contents.Length < 1)
                    return false;
                string imgName = dir + "\\" + img_name;
                System.IO.File.WriteAllBytes(imgName, contents);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        //Get string between  2 strings
        private string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            log("140");
            int Pos2 = STR.IndexOf(LastString);
            log("141");
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            log("142");
            return FinalString;
        }
        private string get_month(string str)
        {
            Dictionary<string, string> months = new Dictionary<string, string>()
            {
                            { "january", "01"},
                            { "february", "02"},
                            { "march", "03"},
                            { "april", "04"},
                            { "may", "05"},
                            { "june", "06"},
                            { "july", "07"},
                            { "august", "08"},
                            { "september", "09"},
                            { "october", "10"},
                            { "november", "11"},
                            { "december", "12"},
            };
            log("143");
            string val = string.Empty;
            log("144");
            foreach (var month in months)
            {
                if (month.Key.Contains(str.ToLower()))
                {
                    val = month.Value;
                    log("145");
                    break;
                }
            }
            return val;

        }

        // Download pdf file
        private void downloadPdf(string url, string dir)
        {
            if (!File.Exists(dir))
            {
                try
                {

                    HttpResponseMessage responseMessage = client.GetAsync(url).Result;
                    log("146");
                    if (responseMessage.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }
                    byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                    log("147");
                    if (contents == null || contents.Length < 1)
                        return;
                    System.IO.File.WriteAllBytes(dir, contents);
                    log("148");
                }
                catch (Exception e)
                {
                }
            }


        }
    }
}
