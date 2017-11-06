using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Web.UI;

namespace woolScraper_cs_
{

    public class Scraper
    {
        public HttpClient client = null;
        public CookieContainer container = null;
        FileWriter writer = null;
        public string fileName = null;
        public WebClient web = null;
        public string started_date = string.Empty;
        public string ended_date = string.Empty;
        public string str_path = string.Empty;
        public string file_path = string.Empty;
        List<string> retailers = new List<string>();
        List<string> app_list = new List<string>();
        List<string> subsurb_list = new List<string>();
        List<string> name_list = new List<string>();
        List<string> redir_urls = new List<string>(); 
        public string date_end = string.Empty;
        private string date_cur = string.Empty;
        private List<string> date_curList = new List<string>();

        public void initHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler();
            container = new CookieContainer();
            handler.CookieContainer = container;
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        }

        //get store_id
        private string get_storeId(string post_code)
        {
            string store_id = string.Empty;

            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000;
            string timestamp = ticks.ToString();
            long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            tick1 /= 10000;
            string timestamp1 = ticks.ToString();
            string callback = "jQuery17205845032764440767_" + timestamp;
            string location_url = "https://embed.salefinder.com.au/location/search/126/?sensitivity=50&limit=10&callback=" + callback + "&query=" + post_code + "&_=" + timestamp1;
            Uri uri = new Uri(location_url);
            var result = client.GetAsync(uri).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            string json_content = Between(strContent + ")", callback + "(", "))");
            dynamic stuff = JsonConvert.DeserializeObject(json_content);
            dynamic result_list = stuff.result;
            foreach (dynamic item in result_list)
            {
                if (post_code == item.postcode.Value)
                {
                    store_id = item.storeId.Value;
                    break;
                }
            }

            return store_id;
        }

        //get_locationID
        private void get_url(string storeId)
        {
            string redirect_url = string.Empty;
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000;
            string timestamp = ticks.ToString();
            long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            tick1 /= 10000;
            string timestamp1 = ticks.ToString();
            string callback = "jQuery17205845032764440767_" + timestamp;
            string reUrl = "https://embed.salefinder.com.au/catalogues/view/126/?format=json&saleGroup=0&locationId=" + storeId + "&callback=" + callback + "&_=" + timestamp1;
            Uri uri = new Uri(reUrl);
            var result = client.GetAsync(uri).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            string json_content = Between(strContent, callback + "(", ")");
            dynamic stuff = JsonConvert.DeserializeObject(json_content);
            if (stuff.redirect != null)
            {
                redirect_url = stuff.redirect.Value;
                string saleId = Between(redirect_url, "saleId=", "&areaName");
                redir_urls.Add(saleId);
                String content = stuff.content.Value;
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(content);
                IEnumerable<HtmlNode> nodeDiv = doc.DocumentNode.Descendants("div").Where(node => node.Attributes["id"] != null && node.Attributes["id"].Value == "gplus");
                date_cur = doc.DocumentNode.SelectSingleNode(".//div[@class='sale-dates-cell']").InnerText;
                date_curList.Add(date_cur);
            }
            else
            {
                String content = stuff.content.Value;
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(content);
                IEnumerable<HtmlNode> nodeDiv = doc.DocumentNode.Descendants("td").Where(node => node.Attributes["class"] != null && node.Attributes["class"].Value == "sale-cell");

                if (nodeDiv == null || nodeDiv.LongCount() < 1)
                    return;

                for (int i = 0; i < nodeDiv.Count(); i++)
                {
                    date_cur = nodeDiv.ToArray()[i].SelectSingleNode(".//div[@class='sale-dates-cell']").InnerText;
                    date_curList.Add(date_cur);
                    redir_urls.Add(Between(nodeDiv.ToArray()[i].SelectSingleNode(".//div[@class='g-plusone']").Attributes["data-href"].Value + "xx", "http://embed.salefinder.com.au/redirect/sale/", "xx"));
                }
            }

        }
        private string convertDay(string day)
        {
            string numDay = string.Empty;
            if (day.Contains("st"))
            {
                numDay = day.Replace("1st", "01");

            }
            else if (day.Contains("nd"))
            {
                numDay = day.Replace("2nd", "02");

            }
            else if (day.Contains("rd"))
            {
                numDay = day.Replace("3rd", "03");
            }
            else if (day.Contains("th"))
            {
                // for instruction
                numDay = "0" + day.Replace("th", "");
                if (numDay.Length == 3)
                {
                    numDay = numDay.Remove(1);
                }
            }
            else if (day.Length == 1)
            {
                numDay = "0" + day;

            }
            else
            {
                numDay = day;
            }


            return numDay;


        }

        public bool woolScrap(string str, string root_dir, string postcode, string file_name)
        {
            initHttpClient();
            string first_row = "CATALOGUE_START" + ',' + "CATALOGUE_END" + ',' + "RETAILER" + ',' + "Description" + ',' + "Page" + ',' + "Promo Type" + ',' + "Multibuy Qty" + ',' + "Price Per Unit" + ',' + "Price Discount";
            string refer_uri = "https://www.woolworths.com.au/shop/catalogue";
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", refer_uri);
            var str_array = new List<string>();
            var page_array = new List<string>();

            string retailer = string.Empty;
            List<string> url_list = new List<string>();
            try
            {
                //get timestamp and callback
                long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
                ticks /= 10000;
                string timestamp = ticks.ToString();
                long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
                tick1 /= 10000;
                string timestamp1 = ticks.ToString();
                string callback = "jQuery17205845032764440767_" + timestamp;
                //
                //get real scraping url
                retailer = str;
                string store_id = get_storeId(postcode);
                get_url(store_id);
                string ppname = string.Empty;
                ppname = "started";
                FileWriter writer1 = new FileWriter("error.txt");

                foreach (string saleId in redir_urls)
                {
                    string url = "https://embed.salefinder.com.au/catalogue/svgData/" + saleId + "/?format=json&pagetype=catalogue&retailerId=126&saleGroup=0&locationId=" + store_id + "&size=960&preview=&callback=" + callback + "&_=" + timestamp1;
                    url_list.Add(url);

                }
                for (int j = 0; j < url_list.Count; j++)
                {
                    if (j == 1)
                    {
                        if (url_list[0] == url_list[1])
                        {
                            break;
                        }
                    }
                    Uri uri = new Uri(url_list[j]);
                    var result = client.GetAsync(uri).Result;
                    result.EnsureSuccessStatusCode();
                    string strContent = result.Content.ReadAsStringAsync().Result;
                    string json_content = Between(strContent + "xx", callback + "(", ")xx");
                    if (json_content.Contains("Sorry, catalogue not found"))
                    {
                        continue;
                    }
                    dynamic stuff = JsonConvert.DeserializeObject(json_content);
                    dynamic content = stuff.catalogue;
                    string pdf_content = stuff.content;
                    //get pdf url
                    pdf_content = pdf_content.Replace("\\\"", "\"");
                    HtmlAgilityPack.HtmlDocument doc1 = new HtmlAgilityPack.HtmlDocument();
                    doc1.LoadHtml(pdf_content);
                    HtmlNode pdf_node = doc1.DocumentNode.SelectSingleNode(".//a[@id='sf-catalogue-download']");
                    if (pdf_node == null)
                    {
                        return false;
                    }
                    string pdf_url = pdf_node.Attributes["href"].Value;
                    //
                    file_path = string.Empty;
                    page_array.Clear();
                    str_array.Clear();
                    for (int i = 0; i < content.Count; i++)
                    {
                        foreach (dynamic cat_item in content[i])
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

                            dynamic item1 = cat_item.Value;
                            if (cat_item.Name == "imagefile")
                            {
                                break;
                            }
                            else if (cat_item.Name == "firstpage")
                            {
                                break;
                            }
                            string item_name = item1.itemName;
                            string product_date = string.Empty;
                            string product_start = string.Empty;
                            string product_end = string.Empty;
                            string date_cur1 = date_curList[j].Replace("Offer valid", "");
                            string[] str_arr = date_cur1.Split('-');
                            string[] str_start = str_arr[0].Trim().Split(' ');
                            string[] str_end = str_arr[1].Trim().Split(' ');
                            string first_month = get_month(str_start[2]);
                            string first_day = convertDay(str_start[1]);
                            string second_day = convertDay(str_end[1]);
                            string second_year = str_end[3];
                            string second_month = get_month(str_end[2]);
                            product_start = first_day + "/" + first_month + "/" + second_year;
                            product_end = second_day + "/" + second_month + "/" + second_year;
                            started_date = product_start;
                            ended_date = product_end;
                            string start_Date = second_year + "_" + first_month + "_" + first_day;
                            if (item_name == null)
                            {
                                string row = started_date + ',' + ended_date + ',' + retailer + ',' + "" + ',' + (i + 1).ToString() + ',' + "" + ',' + "" + ',' + "0" + ',' + "0";
                                writer.WriteRow(row);
                                break;
                            }

                            string item_id = item1.itemId;
                            string page = string.Empty;
                            if (j == 0)
                            {
                                page = (i + 1).ToString();

                            }
                            else
                            {
                                page = (i + 1).ToString();
                            }
                            string itemUrl = "https://embed.salefinder.com.au/item/tooltip/" + item_id + "?callback=" + callback + "&preview=&saleGroup=0&maxWidth=450&maxHeight=460&_=" + timestamp1;
                            var item_content = client.GetAsync(new Uri(itemUrl)).Result;
                            result.EnsureSuccessStatusCode();
                            string item_result = item_content.Content.ReadAsStringAsync().Result;
                            item_result = item_result.Replace("\\\"", "");
                            item_result = item_result.Replace("\\/", "/");
                            item_result = item_result.Replace("\\n", string.Empty);
                            item_result = item_result.Replace("\\t", string.Empty);
                            item_result = item_result.Replace("\\r", string.Empty);
                            item_result = Between(item_result + ")", callback + "(", "))");
                            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(item_result);
                            IEnumerable<HtmlNode> nodeDiv = doc.DocumentNode.Descendants("div").Where(node => node.Attributes["id"] != null && node.Attributes["id"].Value == "sf-item-tooltip");
                            if (nodeDiv == null || nodeDiv.LongCount() < 1)
                                return false;

                            HtmlNode nodeImg = nodeDiv.ToArray()[0].SelectSingleNode(".//a[@id='sf-item-tooltip-image']");
                            string product_des = string.Empty;
                            string product_disprice = string.Empty;
                            string product_regpirce = string.Empty;
                            string promo_type = string.Empty;
                            string image_url = string.Empty;
                            string multibuy_Qty = string.Empty;
                            float qty = 0;
                            item_name = HttpUtility.HtmlDecode(item_name);
                            item_name = filterDescription(item_name);
                            item_name = item_name.Trim();
                            product_des = item_name;
                            if (product_des.Contains("NEW"))
                            {
                                product_des = product_des.Replace("NEW", "").TrimStart();
                                promo_type = "New Line";
                            }
                            image_url = nodeImg.SelectSingleNode(".//img").Attributes["src"].Value;
                            image_url = image_url.Replace("thumbs/ipad", "products");

                            //get product informations
                            HtmlNode nodeDes = nodeDiv.ToArray()[0].SelectSingleNode(".//div[@id='sf-item-tooltip-details-container']");
                            if (nodeDes != null)
                            {

                                if (j == 0)
                                {
                                    string path = file_name.Replace("_YYYY_MM_DD", "");
                                    file_name = path;
                                    file_name = file_name.Replace(" ", "");
                                    file_path = file_name + "_" + start_Date + ".csv";
                                    createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                    downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                    writer = new FileWriter(root_dir + "//" + file_path);
                                    writer.WriteData(first_row, root_dir + "//" + file_path);
                                    downloadPdf(pdf_url, root_dir + "//" + file_name + "_" + start_Date + ".pdf");

                                }
                                else if (date_curList[j] == date_curList[j - 1])
                                {
                                    string path = file_name.Replace("_YYYY_MM_DD", "");
                                    file_name = path;
                                    file_name = file_name.Replace(" ", "");
                                    createDrectory(root_dir + "\\" + file_name + "_" + start_Date + "-1");
                                    downloadFile(root_dir + "\\" + file_name + "_" + start_Date + "-1", product_des, image_url);
                                    file_path = file_name + "_" + start_Date + "-1" + ".csv";
                                    file_path = file_path.Trim();
                                    str_path = file_path;
                                    writer = new FileWriter(root_dir + "//" + file_path);
                                    writer.WriteData(first_row, root_dir + "//" + file_path);
                                    downloadPdf(pdf_url, root_dir + "//" + file_name + "_" + start_Date + "-1" + ".pdf");

                                }
                                else
                                {
                                    string path = file_name.Replace("_YYYY_MM_DD", "");
                                    file_name = path;
                                    file_name = file_name.Replace(" ", "");
                                    file_path = file_name + "_" + start_Date + ".csv";
                                    createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                    downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                    writer = new FileWriter(root_dir + "//" + file_path);
                                    writer.WriteData(first_row, root_dir + "//" + file_path);
                                    downloadPdf(pdf_url, root_dir + "//" + file_name + "_" + start_Date + ".pdf");

                                }


                                if (nodeDes.SelectSingleNode(".//span[@class='sf-pricedisplay']") != null)
                                {
                                    product_disprice = nodeDes.SelectSingleNode(".//span[@class='sf-pricedisplay']").InnerText;
                                    product_disprice = product_disprice.Replace("$", "");
                                }
                                if (nodeDes.SelectSingleNode(".//span[@class='sf-regprice']") != null)
                                {
                                    product_regpirce = nodeDes.SelectSingleNode(".//span[@class='sf-regprice']").InnerText;
                                    product_regpirce = product_regpirce.Replace("$", "");

                                }

                                string multi = string.Empty;
                                if (nodeDes.SelectSingleNode(".//span[@class='sf-nowprice']") == null)
                                {

                                }
                                else
                                {

                                    multi = nodeDes.SelectSingleNode(".//span[@class='sf-nowprice']").InnerText;

                                }
                                if (multi.Contains("Any") && multi.Contains("for"))
                                {
                                    multibuy_Qty = Between(multi, "Any", "for").Trim();
                                    qty = float.Parse(multibuy_Qty);
                                    // product_disprice = Between("/" + product_disprice, "/", ".");
                                    product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                    product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                }
                                else if (multi.Contains("Both for"))
                                {

                                }
                                else if (multi.Contains("All") && multi.Contains("for"))
                                {
                                    multibuy_Qty = Between(multi, "All", "for").Trim();
                                    qty = float.Parse(multibuy_Qty);
                                    // product_disprice = Between("/" + product_disprice, "/", ".");
                                    product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                    product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");

                                }
                                else if (multi.Contains("for"))
                                {
                                    multibuy_Qty = Between("Any" + multi, "Any", "for").Trim();
                                    qty = float.Parse(multibuy_Qty);
                                    //product_disprice = Between("/" + product_disprice, "/", ".");
                                    if ((float.Parse(product_disprice) / qty).ToString("N2") == "0.50")
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        product_regpirce = "0";

                                    }
                                    else
                                    {
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");

                                    }

                                }

                                else if (multi.Contains("each"))
                                {

                                }
                                float dis_price = 0;
                                float reg_price = 0;

                                if (product_regpirce == string.Empty)
                                {
                                    reg_price = 0;
                                }
                                else
                                {
                                    reg_price = float.Parse(product_regpirce);

                                }
                                if (product_disprice == string.Empty)
                                {
                                    dis_price = 0;
                                }
                                else
                                {
                                    dis_price = float.Parse(product_disprice);

                                }
                                if (nodeDes.SelectSingleNode(".//span[@class='sf-regoption']") != null)
                                {
                                    if (nodeDes.SelectSingleNode(".//span[@class='sf-regoption']").InnerText.Contains("Was"))
                                    {
                                        reg_price = reg_price - dis_price;
                                        product_regpirce = reg_price.ToString("N2");
                                    }
                                    else if (nodeDes.SelectSingleNode(".//span[@class='sf-regoption']").InnerText.Contains("Introductory price"))
                                    {
                                        product_regpirce = "0";
                                    }
                                }

                                if (qty > 1)
                                {
                                    promo_type = "Multibuy";
                                }
                                else if (reg_price >= dis_price)
                                {
                                    if (promo_type != "New Line")
                                    {
                                        promo_type = "Half Price";

                                    }

                                }
                                else
                                {
                                    if (promo_type != "New Line")
                                    {
                                        promo_type = "Price Reduction";
                                    }
                                }
                                if (j == 1)
                                {
                                    string row = started_date + ',' + ended_date + ',' + retailer + ',' + product_des + ',' + page + ',' + promo_type + ',' + multibuy_Qty + ',' + product_disprice + ',' + product_regpirce;
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
                                            int key = str_array.IndexOf(product_des);
                                            if (page_array[key] == page)
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
                                            int key = str_array.IndexOf(product_des);
                                            if (page_array[key] == page)
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

                }          



              
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;

        }

        private string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            return FinalString;
        }

        public void downloadFile(string dir, string img_name, string link)
        {
            try
            {

                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }
                byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                if (contents == null || contents.Length < 1)
                    return;
                string imgName = dir + "\\" + img_name + ".jpg";
                System.IO.File.WriteAllBytes(imgName, contents);
            }
            catch (Exception e)
            {
            }
        }

        public string filterDescription(string originItemName)
        {
            string filteredItemName = "";
            if (originItemName.Contains("*"))
            {
                originItemName = originItemName.Replace("*", "");


            }
            if (originItemName.Contains("#"))
            {
                originItemName = originItemName.Replace("#", "");

            }
            else if (originItemName.Contains("Range"))
            {
                originItemName = originItemName.Replace("Range", "");
            }
            else if (originItemName.Contains("selected"))
            {
                originItemName = originItemName.Replace("selected", "");
            }
            else if (originItemName.Contains("Selected"))
            {
                originItemName = originItemName.Replace("Selected", "");
            }
            else if (originItemName.Contains("- from the deli dept"))
            {
                originItemName = originItemName.Replace("- from the deli dept", "");
            }
            else if (originItemName.Contains("– From the Deli"))
            {
                originItemName = originItemName.Replace("– From the Deli", "");
            }
            if (originItemName.Contains("é"))
            {
                originItemName = originItemName.Replace("é", "e");

            }
            if (originItemName.Contains("`"))
            {
                originItemName = originItemName.Replace("`", "'");

            }
            if (originItemName.Contains("'"))
            {
                originItemName = originItemName.Replace("'", "'");

            }
            if (originItemName.Contains("‘"))
            {
                originItemName = originItemName.Replace("‘", "'");

            }
            if (originItemName.Contains("Varieties"))
            {
                originItemName = originItemName.Replace("Varieties", "");

            }
            if (originItemName.Contains("varieties"))
            {
                originItemName = originItemName.Replace("varieties", "");

            }
            if (originItemName.Contains("–"))
            {
                originItemName = originItemName.Replace("–", "-");

            }
            if (originItemName.Contains(","))
            {
                originItemName = "\"" + originItemName + "\"";
            }
            if (originItemName.Contains("/"))
            {
                originItemName = originItemName.Replace("/", "");
            }
            if (originItemName.Contains("NEW"))
            {
                originItemName = originItemName.Replace("NEW", "").TrimStart();
            }
            originItemName = originItemName.Replace("&amp;", "");
            originItemName = originItemName.Replace("ü", "u");
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
        public void createDrectory(string name)
        {

            bool exists = System.IO.Directory.Exists(name);

            if (!exists)
                Directory.CreateDirectory(name);
        }

        public string get_month(string str)
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

        public void downloadPdf(string url, string dir)
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
