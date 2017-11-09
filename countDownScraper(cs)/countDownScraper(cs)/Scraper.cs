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
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using countDownScraper_cs_.Json;

namespace countDownScraper_cs_
{
    public class Scraper
    {
        private HttpClient client = null;
        FileWriter writer = null;
        private string fileName = null;
        private WebClient web = null;
        private string str_path = string.Empty;
        private string file_path = string.Empty;
        private string pdf_falePath = string.Empty;
        List<string> retailers = new List<string>();
        List<string> app_list = new List<string>();
        List<string> subsurb_list = new List<string>();
        List<string> name_list = new List<string>();
        List<string> Url_list = new List<string>();
        List<string> mainUrl_list = new List<string>();
        List<long> page_list = new List<long>();
        List<string> cat_ids = new List<string>();
        private string date_end = string.Empty;
        private string store_id = string.Empty;
        private string store_name = string.Empty;
        List<string> startDate = new List<string>();
        List<string> endDate = new List<string>();
        //Initialize httpclient
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
        private void get_publicUrl(string post_code, string referUrl)
        {
            string location = string.Empty;
            if (post_code == "North Island")
            {
                location = "south-island";
            }
            else if (post_code == "South Island")
            {
                location = "north-island";
            }
            client.DefaultRequestHeaders.Referrer = null;
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", referUrl);
            string location_url = "https://www.countdown.co.nz/api/CatalogueServer/GetCurrentCataloguesExcludingThisToken?catalogueName=" + location;
            Uri uri = new Uri(location_url);
            var result = client.GetAsync(uri).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            dynamic stuff = JsonConvert.DeserializeObject(strContent);
            dynamic arr = stuff.catalogues;
            mainUrl_list.Clear();
            foreach (dynamic item in arr)
            {
                mainUrl_list.Add(item.public_url.Value);
                page_list.Add(item.page_count.Value);
                DateTime startdate = item.schedule_online_at.Value;
                startdate = startdate.AddHours(14);
                DateTime enddate = item.schedule_offline_at.Value;
                startDate.Add(startdate.ToString());
                //startDate = startDate.Split(' ')[0];
                endDate.Add(enddate.ToString());
                //endDate = endDate.Split(' ')[0];

            }


        }


        private string regStr(string content, string pattern, string replaceStr)
        {
            string result = string.Empty;
            Regex rgx = new Regex(pattern);
            result = rgx.Replace(content, replaceStr);

            return result;

        }

        //Main scraping
        public  bool countdownScrap(string str, string root_dir, string storeUrl, string postcode, string file_name)
        {
            CookieContainer container = null;
            initHttpClient(container);
            string first_row = "CATALOGUE_START" + ',' + "CATALOGUE_END" + ',' + "RETAILER" + ',' + "Description" + ',' + "Page" + ',' + "Promo Type" + ',' + "Multibuy Qty" + ',' + "Price Per Unit" + ',' + "Price Discount";

            var str_array = new List<string>();
            var page_array = new List<string>();
            string retailer = string.Empty;
            string ppname = string.Empty;
            ppname = "started";
            startDate.Clear();
            endDate.Clear();

            try
            {
                var firstResult = client.GetAsync(storeUrl).Result;
                firstResult.EnsureSuccessStatusCode();
                string firstContent = firstResult.Content.ReadAsStringAsync().Result;

                //get real scraping url
                retailer = str;
                get_publicUrl(postcode, storeUrl);

                for (int j = 0; j < mainUrl_list.Count; j++)
                {
                    string product_date = string.Empty;
                    string product_start = string.Empty;
                    string product_end = string.Empty;
                    string start_Date = string.Empty;
                    string pdfUrl = string.Empty;
                    str_array.Clear();
                    page_array.Clear();
                    client.DefaultRequestHeaders.Referrer = null;
                    Uri uri = new Uri(mainUrl_list[j]);
                    var result = client.GetAsync(uri).Result;
                    result.EnsureSuccessStatusCode();
                    string strContent = result.Content.ReadAsStringAsync().Result;
                    strContent = strContent.Replace("\n", "");
                    strContent = strContent.Replace("\t", "");
                    strContent = Between(strContent, "var data =", ";      Reader.Bootstrap");

                    //
                    dynamic jsonContent = JsonConvert.DeserializeObject(strContent);
                    if (jsonContent.config.downloadPdfUrl == null)
                    {
                    }
                    else
                    {
                        pdfUrl = jsonContent.config.downloadPdfUrl.Value;

                    }
                    string[] strArray3 = null;
                    string[] strArray4 = null;
                    string first_day = string.Empty;
                    string end_day = string.Empty;
                    first_day = startDate[j].Split(' ')[0];
                    end_day = endDate[j].Split(' ')[0];
                    if (first_day.Contains('/'))
                    {
                        strArray3 = first_day.Trim().Split('/');
                        strArray4 = end_day.Trim().Split('/');
                    }
                    else if (first_day.Contains('-'))
                    {
                        strArray3 = first_day.Trim().Split('-');
                        strArray4 = end_day.Trim().Split('-');
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

                    product_start = d + "/" + m + "/" + strArray3[2];
                    product_end = d1 + "/" + m1 + "/" + strArray4[2];
                    FileWriter writer1 = new FileWriter("error.txt");


                    Result json = JsonConvert.DeserializeObject<Result>(strContent);
                    if (json == null || json.config == null || json.spreads == null || json.spreads.Count < 1)
                        continue;

                    List<ResultHotspot> lHotspots = new List<ResultHotspot>();
                    List<ResultHotspot> rHotspots = new List<ResultHotspot>();
                    List<ResultHotspot> lHotspotsSort = new List<ResultHotspot>();
                    List<ResultHotspot> rHotspotsSort = new List<ResultHotspot>();
                    List<List<ResultHotspot>> totalspots = new List<List<ResultHotspot>>();
                    int k = 0;
                    foreach (ResultSpread rSpread in json.spreads)
                    {
                        totalspots.Clear();
                        lHotspotsSort.Clear();
                        if (rSpread.pages.Count == 1)
                        {
                            lHotspotsSort = rSpread.hotspots.OrderBy(o => o.position.top).ToList();
                            totalspots.Add(lHotspotsSort);
                        }
                        else if (rSpread.pages.Count == 2)
                        {
                            totalspots.Clear();
                            lHotspots.Clear();
                            rHotspots.Clear();
                            lHotspotsSort.Clear();
                            rHotspotsSort.Clear();
                            foreach (ResultHotspot rHotspot in rSpread.hotspots)
                            {
                                if (rHotspot.position.left < 0.5)
                                {
                                    lHotspots.Add(rHotspot);

                                }
                                else
                                {
                                    rHotspots.Add(rHotspot);
                                }

                            }
                            lHotspotsSort = lHotspots.OrderBy(o => o.position.top).ToList();
                            rHotspotsSort = rHotspots.OrderBy(o => o.position.top).ToList();
                            totalspots.Add(lHotspotsSort);
                            totalspots.Add(rHotspotsSort);
                        }
                        for (int kk = 0; kk < totalspots.Count; kk++)
                        {
                            k++;
                            foreach (ResultHotspot jsonItem in totalspots[kk])
                            {
                                if (jsonItem.type == null)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (jsonItem.type == "product")
                                    {
                                        if (jsonItem.products[0].id != null)
                                        {
                                            try
                                            {
                                                if (File.Exists(root_dir + "//" + file_path))
                                                {
                                                    using (var stream = new StreamReader(root_dir + "//" + file_path))
                                                    {
                                                        while (!stream.EndOfStream)
                                                        {
                                                            var splits = stream.ReadLine().Split(',');
                                                            str_array.Add(splits[3]);
                                                            page_array.Add(splits[4]);

                                                        }

                                                    }

                                                }

                                                string fileDate = strArray3[2] + "_" + m + "_" + d;
                                                string path = file_name.Replace("_YYYY_MM_DD", "");
                                                file_name = path.Replace(" ", "");
                                                if (j == 0)
                                                {
                                                    file_path = file_name + "_" + fileDate + ".csv";
                                                    createDrectory(root_dir + "\\" + file_name + "_" + fileDate);
                                                    downloadPdf(pdfUrl, root_dir + "//" + file_name + "_" + fileDate + ".pdf");
                                                    writer = new FileWriter(root_dir + "//" + file_path);
                                                    writer.WriteData(first_row, root_dir + "//" + file_path);
                                                }
                                                else
                                                {
                                                    if (startDate[j] == startDate[j - 1])
                                                    {
                                                        file_path = file_name + "_" + fileDate + "-" + j.ToString() + ".csv";
                                                        createDrectory(root_dir + "\\" + file_name + "_" + fileDate + "-" + j.ToString());
                                                        downloadPdf(pdfUrl, root_dir + "//" + file_name + "_" + fileDate + "-" + j.ToString() + ".pdf");
                                                        writer = new FileWriter(root_dir + "//" + file_path);
                                                        writer.WriteData(first_row, root_dir + "//" + file_path);

                                                    }
                                                    else
                                                    {
                                                        createDrectory(root_dir + "\\" + file_name + "_" + fileDate);
                                                        file_path = file_name + "_" + fileDate + ".csv";
                                                        downloadPdf(pdfUrl, root_dir + "//" + file_name + "_" + fileDate + ".pdf");
                                                        writer = new FileWriter(root_dir + "//" + file_path);
                                                        writer.WriteData(first_row, root_dir + "//" + file_path);
                                                    }


                                                }
                                                string id = jsonItem.products[0].id;
                                                string itemUrl = mainUrl_list[j] + "product/" + id + ".json";
                                                string page = string.Empty;
                                                client.DefaultRequestHeaders.Referrer = new Uri(mainUrl_list[j] + "page/" + k.ToString());
                                                var itemresult = client.GetAsync(itemUrl).Result;
                                                itemresult.EnsureSuccessStatusCode();
                                                string itemStr = itemresult.Content.ReadAsStringAsync().Result;
                                                dynamic json5 = JsonConvert.DeserializeObject(itemStr);
                                                string item_title = json5.title.Value;
                                                page = k.ToString();
                                                if (json5.webshopUrl != null)
                                                {
                                                    string mainUrl = json5.webshopUrl.Value;
                                                    if (mainUrl == "http://www.countdown.co.nz/shopping-made-easy/countdown-pharmacy")
                                                    {
                                                        if (item_title.Contains("*"))
                                                        {
                                                            item_title = item_title.Replace("*", "");


                                                        }
                                                        if (item_title.Contains("#"))
                                                        {
                                                            item_title = item_title.Replace("#", "");

                                                        }
                                                        else if (item_title.Contains("Range"))
                                                        {
                                                            item_title = item_title.Replace("Range", "");
                                                        }
                                                        else if (item_title.Contains("selected"))
                                                        {
                                                            item_title = item_title.Replace("selected", "");
                                                        }
                                                        else if (item_title.Contains("Selected"))
                                                        {
                                                            item_title = item_title.Replace("Selected", "");
                                                        }
                                                        else if (item_title.Contains("- from the deli dept"))
                                                        {
                                                            item_title = item_title.Replace("- from the deli dept", "");
                                                        }
                                                        else if (item_title.Contains("– From the Deli"))
                                                        {
                                                            item_title = item_title.Replace("– From the Deli", "");
                                                        }
                                                        if (item_title.Contains("é"))
                                                        {
                                                            item_title = item_title.Replace("é", "e");

                                                        }
                                                        if (item_title.Contains("`"))
                                                        {
                                                            item_title = item_title.Replace("`", "'");

                                                        }
                                                        if (item_title.Contains("'"))
                                                        {
                                                            item_title = item_title.Replace("'", "'");

                                                        }
                                                        if (item_title.Contains("‘"))
                                                        {
                                                            item_title = item_title.Replace("‘", "'");

                                                        }
                                                        if (item_title.Contains("Varieties"))
                                                        {
                                                            item_title = item_title.Replace("Varieties", "");

                                                        }
                                                        if (item_title.Contains("varieties"))
                                                        {
                                                            item_title = item_title.Replace("varieties", "");

                                                        }
                                                        item_title = filterDescription(item_title);
                                                        item_title = item_title.Trim();
                                                        ppname = item_title;
                                                        if (item_title.Contains("–"))
                                                        {
                                                            item_title = item_title.Replace("–", "-");

                                                        }
                                                        if (item_title.Contains(","))
                                                        {
                                                            item_title = "\"" + item_title + "\"";
                                                        }
                                                        if (item_title.Contains("/"))
                                                        {
                                                            item_title = item_title.Replace("/", "");
                                                        }
                                                        if (item_title.Contains("NEW"))
                                                        {
                                                            item_title = item_title.Replace("NEW", "").TrimStart();
                                                            item_title = "New Line";
                                                        }
                                                        item_title = item_title.Replace("&amp;", "");
                                                        string row = product_start + ',' + product_end + ',' + retailer + ',' + item_title + ',' + page + ',' + "" + ',' + "" + ',' + "0" + ',' + "0";
                                                        if (str_array.Count == 1)
                                                        {
                                                            writer.WriteRow(row);

                                                        }
                                                        else
                                                        {
                                                            if (!str_array.Contains(item_title))
                                                            {
                                                                writer.WriteRow(row);
                                                            }
                                                            else
                                                            {
                                                                int key = str_array.IndexOf(item_title);
                                                                if (page_array[key] == k.ToString())
                                                                {

                                                                }
                                                                else
                                                                {
                                                                    writer.WriteRow(row);
                                                                }

                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        string product_des = string.Empty;
                                                        client.DefaultRequestHeaders.Referrer = new Uri(itemUrl);
                                                        var mainResult = client.GetAsync(mainUrl).Result;
                                                        mainResult.EnsureSuccessStatusCode();
                                                        string mainStr = mainResult.Content.ReadAsStringAsync().Result;
                                                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                                                        doc.LoadHtml(mainStr);
                                                        IEnumerable<HtmlNode> products = doc.DocumentNode.Descendants("div").Where(node => node.Attributes["class"] != null && node.Attributes["class"].Value == "product-stamp product-stamp-grid");
                                                        product_des = item_title;
                                                        if (product_des.Contains("*"))
                                                        {
                                                            product_des = product_des.Replace("*", "");


                                                        }
                                                        if (product_des.Contains("#"))
                                                        {
                                                            product_des = product_des.Replace("#", "");

                                                        }
                                                        else if (product_des.Contains("Range"))
                                                        {
                                                            product_des = product_des.Replace("Range", "");
                                                        }
                                                        else if (product_des.Contains("selected"))
                                                        {
                                                            product_des = product_des.Replace("selected", "");
                                                        }
                                                        else if (product_des.Contains("Selected"))
                                                        {
                                                            product_des = product_des.Replace("Selected", "");
                                                        }
                                                        else if (product_des.Contains("- from the deli dept"))
                                                        {
                                                            product_des = product_des.Replace("- from the deli dept", "");
                                                        }
                                                        else if (product_des.Contains("– From the Deli"))
                                                        {
                                                            product_des = product_des.Replace("– From the Deli", "");
                                                        }
                                                        if (product_des.Contains("é"))
                                                        {
                                                            product_des = product_des.Replace("é", "e");

                                                        }
                                                        if (product_des.Contains("`"))
                                                        {
                                                            product_des = product_des.Replace("`", "'");

                                                        }
                                                        if (product_des.Contains("'"))
                                                        {
                                                            product_des = product_des.Replace("'", "'");

                                                        }
                                                        if (product_des.Contains("‘"))
                                                        {
                                                            product_des = product_des.Replace("‘", "'");

                                                        }
                                                        if (product_des.Contains("Varieties"))
                                                        {
                                                            product_des = product_des.Replace("Varieties", "");

                                                        }
                                                        if (product_des.Contains("varieties"))
                                                        {
                                                            product_des = product_des.Replace("varieties", "");

                                                        }
                                                        product_des = product_des.Replace("&amp;", "");
                                                        product_des = filterDescription(product_des);
                                                        product_des = product_des.Trim();
                                                        ppname = product_des;
                                                        if (product_des.Contains("–"))
                                                        {
                                                            product_des = product_des.Replace("–", "-");

                                                        }
                                                        if (product_des.Contains(","))
                                                        {
                                                            product_des = "\"" + product_des + "\"";
                                                        }
                                                        if (product_des.Contains("/"))
                                                        {
                                                            product_des = product_des.Replace("/", "");
                                                        }


                                                        if (doc.DocumentNode.SelectNodes(".//div[@class='product-stamp product-stamp-grid']") == null)
                                                        {
                                                            string row = product_start + ',' + product_end + ',' + retailer + ',' + product_des + ',' + page + ',' + "Price Reduction" + ',' + "" + ',' + "0" + ',' + "0";
                                                            if (!str_array.Contains(product_des))
                                                            {
                                                                writer.WriteRow(row);
                                                            }
                                                            else
                                                            {
                                                                int key = str_array.LastIndexOf(product_des);

                                                                if (page_array[key] == k.ToString())
                                                                {

                                                                }
                                                                else
                                                                {
                                                                    writer.WriteRow(row);
                                                                }

                                                            }

                                                            continue;
                                                        }
                                                        HtmlNodeCollection collection = doc.DocumentNode.SelectNodes(".//div[@class='product-stamp product-stamp-grid']");
                                                        IEnumerable<HtmlNode> hoveritems = collection.Descendants("span").Where(node2 => node2.InnerText == item_title && node2.Attributes["class"].Value == "description span12 mspan8");
                                                        HtmlNode itemNode = null;
                                                        if (hoveritems == null || hoveritems.Count() == 0)
                                                        {
                                                            itemNode = products.ToArray()[0];
                                                        }
                                                        else
                                                        {
                                                            itemNode = hoveritems.ToArray()[0].ParentNode.ParentNode.ParentNode;
                                                        }
                                                        string proudct_img = string.Empty;
                                                        string product_disprice = string.Empty;
                                                        string product_regpirce = string.Empty;
                                                        string promo_type = string.Empty;
                                                        string multibuy_Qty = string.Empty;
                                                        float qty = 0;


                                                        //

                                                        if (itemNode.SelectSingleNode(".//img[@class='product-image']").Attributes["src"] != null)
                                                        {
                                                            proudct_img = "https://shop.countdown.co.nz" + itemNode.SelectSingleNode(".//img[@class='product-image']").Attributes["src"].Value;
                                                            proudct_img = proudct_img.Replace("/big/", "/large/");

                                                        }
                                                        if (product_des.Contains("NEW"))
                                                        {
                                                            product_des = product_des.Replace("NEW", "").TrimStart();
                                                            promo_type = "New Line";
                                                        }

                                                        if (itemNode.SelectSingleNode(".//span[@class='price din-medium']") != null)
                                                        {
                                                            product_disprice = itemNode.SelectSingleNode(".//span[@class='price din-medium']").InnerText;
                                                            product_disprice = product_disprice.Replace("\r", "");
                                                            product_disprice = product_disprice.Replace("\n", "");
                                                            product_disprice = product_disprice.Replace("&nbsp", "");
                                                            product_disprice = product_disprice.Replace(";ea", "");
                                                            product_disprice = product_disprice.Replace("$", "").Trim();
                                                            product_disprice = product_disprice.Replace("kg", "").Trim();
                                                            product_disprice = product_disprice.Replace("g", "").Trim();
                                                            product_disprice = product_disprice.Replace("pk", "").Trim();
                                                            product_disprice = product_disprice.Replace(";", "");


                                                        }
                                                        else if (itemNode.SelectSingleNode(".//span[@class='club-price-wrapper club-text-colour din-medium']") != null)
                                                        {
                                                            product_disprice = itemNode.SelectSingleNode(".//span[@class='club-price-wrapper club-text-colour din-medium']").InnerText;
                                                            product_disprice = product_disprice.Replace("\r", "");
                                                            product_disprice = product_disprice.Replace("\n", "");
                                                            product_disprice = product_disprice.Replace("&nbsp", "");
                                                            product_disprice = product_disprice.Replace(";ea", "");
                                                            product_disprice = product_disprice.Replace("$", "").Trim();
                                                            product_disprice = product_disprice.Replace("kg", "").Trim();
                                                            product_disprice = product_disprice.Replace("g", "").Trim();
                                                            product_disprice = product_disprice.Replace("pk", "").Trim();
                                                            product_disprice = product_disprice.Replace(";", "");

                                                        }
                                                        else if (itemNode.SelectSingleNode(".//span[@class='price special-price din-medium savings-text']") != null)
                                                        {
                                                            product_disprice = itemNode.SelectSingleNode(".//span[@class='price special-price din-medium savings-text']").InnerText;
                                                            product_disprice = product_disprice.Replace("\r", "");
                                                            product_disprice = product_disprice.Replace("\n", "");
                                                            product_disprice = product_disprice.Replace("&nbsp", "");
                                                            product_disprice = product_disprice.Replace(";ea", "");
                                                            product_disprice = product_disprice.Replace("$", "").Trim();
                                                            product_disprice = product_disprice.Replace("kg", "").Trim();
                                                            product_disprice = product_disprice.Replace("g", "").Trim();
                                                            product_disprice = product_disprice.Replace("pk", "").Trim();
                                                            product_disprice = product_disprice.Replace(";", "");
                                                        }
                                                        else
                                                        {
                                                            product_disprice = "0";
                                                        }

                                                        if (itemNode.SelectSingleNode(".//span[@class='multi-buy-award-quantity']") != null)
                                                        {
                                                            multibuy_Qty = itemNode.SelectSingleNode(".//span[@class='multi-buy-award-quantity']").InnerText;
                                                            if (multibuy_Qty.Contains("buy"))
                                                            {
                                                                qty = 1;
                                                            }
                                                            if (multibuy_Qty.Contains("any") && multibuy_Qty.Contains("for"))
                                                            {
                                                                multibuy_Qty = convertNum(multibuy_Qty);
                                                                multibuy_Qty = Between(multibuy_Qty, "any", "for").Trim();
                                                                qty = float.Parse(multibuy_Qty);

                                                            }
                                                            else if (multibuy_Qty.Contains("all") && multibuy_Qty.Contains("for"))
                                                            {
                                                                multibuy_Qty = convertNum(multibuy_Qty);
                                                                multibuy_Qty = Between(multibuy_Qty, "all", "for").Trim();
                                                                qty = float.Parse(multibuy_Qty);

                                                            }
                                                            else if (multibuy_Qty.Contains("both for"))
                                                            {
                                                                qty = 1;
                                                            }
                                                            else if (multibuy_Qty.Contains("buy") && multibuy_Qty.Contains("for"))
                                                            {
                                                                multibuy_Qty = convertNum(multibuy_Qty);
                                                                multibuy_Qty = Between(multibuy_Qty, "buy", "for").Trim();
                                                                qty = float.Parse(multibuy_Qty);

                                                            }
                                                            else if (multibuy_Qty.Contains("for"))
                                                            {

                                                                multibuy_Qty = convertNum(multibuy_Qty);
                                                                multibuy_Qty = Between("any" + multibuy_Qty, "any", "for").Trim();
                                                                qty = float.Parse(multibuy_Qty);

                                                            }

                                                            else if (multibuy_Qty.Contains("each"))
                                                            {
                                                                qty = 1;
                                                            }

                                                        }
                                                        else
                                                        {
                                                            qty = 1;
                                                        }

                                                        if (itemNode.SelectSingleNode(".//span[@class='multi-buy-award-value has-cents']") != null)
                                                        {
                                                            if (itemNode.SelectSingleNode(".//span[@class='multi-buy-award-value has-cents']") != null)
                                                            {
                                                                product_regpirce = itemNode.SelectSingleNode(".//span[@class='multi-buy-award-value has-cents']").InnerText;
                                                                string temp_price = product_regpirce;
                                                                product_regpirce = (float.Parse(product_disprice) - float.Parse(product_regpirce) / qty).ToString("N2");
                                                                product_disprice = (float.Parse(temp_price) / qty).ToString("N2");
                                                            }

                                                        }
                                                        else if (itemNode.SelectSingleNode(".//span[@class='multi-buy-award-value']") != null)
                                                        {
                                                            product_regpirce = itemNode.SelectSingleNode(".//span[@class='multi-buy-award-value']").InnerText;
                                                            string temp_price = product_regpirce;
                                                            product_regpirce = (float.Parse(product_disprice) - float.Parse(product_regpirce) / qty).ToString("N2");
                                                            product_disprice = (float.Parse(temp_price) / qty).ToString("N2");
                                                        }
                                                        else
                                                        {
                                                            product_regpirce = "0";
                                                        }
                                                        if (itemNode.SelectSingleNode(".//span[@class='was-price hidden-phone']") != null)
                                                        {
                                                            product_regpirce = itemNode.SelectSingleNode(".//span[@class='was-price hidden-phone']").InnerText;
                                                            product_regpirce = product_regpirce.Replace("was", "");
                                                            product_regpirce = product_regpirce.Replace(";", "").Trim();
                                                            product_regpirce = product_regpirce.Replace("$", "");
                                                            product_regpirce = (float.Parse(product_regpirce) - float.Parse(product_disprice)).ToString("N2");
                                                        }

                                                        float dis_price = 0;
                                                        float reg_price = 0;

                                                        if (product_regpirce == "0")
                                                        {
                                                            dis_price = 0;
                                                        }
                                                        else
                                                        {
                                                            dis_price = float.Parse(product_regpirce);

                                                        }
                                                        if (product_disprice == "0")
                                                        {
                                                            reg_price = 0;
                                                        }
                                                        else
                                                        {
                                                            reg_price = float.Parse(product_disprice);

                                                        }

                                                        if (qty > 1.0)
                                                        {
                                                            if (product_regpirce == "0")
                                                            {
                                                                multibuy_Qty = "";
                                                                promo_type = "Price Reduction";
                                                            }
                                                            else
                                                            {
                                                                promo_type = "Multibuy";

                                                            }
                                                        }
                                                        else if (dis_price >= reg_price)
                                                        {
                                                            if (reg_price != 0 && dis_price != 0)
                                                            {
                                                                if (promo_type != "New Line")
                                                                {
                                                                    promo_type = "Half Price";

                                                                }
                                                            }
                                                            else
                                                            {
                                                                promo_type = "Price Reduction";

                                                            }


                                                        }
                                                        else
                                                        {
                                                            if (promo_type != "New Line")
                                                            {
                                                                promo_type = "Price Reduction";
                                                            }
                                                        }

                                                        if (itemNode.SelectSingleNode(".//img[@class='hidden-phone']") != null)
                                                        {
                                                            string src = itemNode.SelectSingleNode(".//img[@class='hidden-phone']").Attributes["src"].Value;
                                                            if (src.Contains("pricelockdown"))
                                                            {
                                                                promo_type = "Price Lockdown";
                                                            }
                                                            else if (src.Contains("onecard"))
                                                            {
                                                                promo_type = "Onecard";
                                                            }
                                                        }

                                                        if (product_disprice == "" && product_regpirce == "")
                                                        {
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            if (j == 0)
                                                            {
                                                                downloadFile(root_dir + "\\" + file_name + "_" + fileDate, product_des, proudct_img);

                                                            }
                                                            else
                                                            {
                                                                if (startDate[j] == startDate[j - 1])
                                                                {

                                                                    downloadFile(root_dir + "\\" + file_name + "_" + fileDate + "-" + j.ToString(), product_des, proudct_img);


                                                                }
                                                                else
                                                                {

                                                                    downloadFile(root_dir + "\\" + file_name + "_" + fileDate, product_des, proudct_img);
                                                                }
                                                            }

                                                            string row = product_start + ',' + product_end + ',' + retailer + ',' + product_des + ',' + page + ',' + promo_type + ',' + multibuy_Qty + ',' + product_disprice + ',' + product_regpirce;
                                                            if (str_array.Count == 1)
                                                            {
                                                                writer.WriteRow(row);

                                                            }
                                                            else
                                                            {
                                                                if (!str_array.Contains(product_des))
                                                                {
                                                                    writer.WriteRow(row);
                                                                }
                                                                else
                                                                {
                                                                    int key = str_array.LastIndexOf(product_des);

                                                                    if (page_array[key] == k.ToString())
                                                                    {

                                                                    }
                                                                    else
                                                                    {
                                                                        writer.WriteRow(row);
                                                                    }

                                                                }
                                                            }
                                                        }

                                                    }


                                                }

                                            }
                                            catch (Exception)
                                            {
                                                writer1.WriteRow(ppname + "," + k.ToString());
                                                continue;
                                            }


                                        }

                                    }
                                }

                            }
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

            if (!exists)
                Directory.CreateDirectory(name);
        }
       
        private string convertNum(string multi)
        {
            string str = multi.Replace("two", "2");
            str = str.Replace("three", "3");
            str = str.Replace("four", "4");
            str = str.Replace("five", "5");
            str = str.Replace("six", "6");
            return str;

        }
        //Download Image file
        private void downloadFile(string dir, string img_name, string link)
        {
            try
            {
                byte[] contents = null;
                string imgName = string.Empty;

                img_name = img_name.Replace("\"", "");
                img_name = img_name.Replace("/", "");

                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    link = link.Replace("/large/", "/big/");
                    responseMessage = client.GetAsync(link).Result;
                    if (responseMessage.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }
                    contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                    if (contents == null || contents.Length < 1)
                        return;
                    imgName = dir + "\\" + img_name + ".jpg";
                    System.IO.File.WriteAllBytes(imgName, contents);
                }
                contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                if (contents == null || contents.Length < 1)
                    return;
                imgName = dir + "\\" + img_name + ".jpg";
                System.IO.File.WriteAllBytes(imgName, contents);
            }
            catch (Exception e)
            {
            }
        }
        private void downloadImage(string dir, string img_name, string link)
        {
            try
            {
                if (img_name.Contains("\""))
                {
                    img_name = img_name.Replace("\"", "");
                }

                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }
                byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                if (contents == null || contents.Length < 1)
                    return;
                string imgName = dir + "\\" + img_name;
                System.IO.File.WriteAllBytes(imgName, contents);
            }
            catch (Exception e)
            {
            }
        }

        //Get string between  2 strings
        private string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
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
            string val = string.Empty;
            foreach (var month in months)
            {
                if (month.Key.Contains(str.ToLower()))
                {
                    val = month.Value;
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
                    if (responseMessage.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }
                    byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                    if (contents == null || contents.Length < 1)
                        return;
                    System.IO.File.WriteAllBytes(dir, contents);
                }
                catch (Exception e)
                {
                }
            }

        }

 
       
    }
}
