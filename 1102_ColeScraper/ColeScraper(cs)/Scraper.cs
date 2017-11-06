using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ColeScraper_cs_
{
    public class Scraper
    {
        private HttpClient client = null;
        private CookieContainer container = null;
        FileWriter writer = null;
        private string fileName = null;
        private WebClient web = null;
        private string started_date = string.Empty;
        private string ended_date = string.Empty;
        private string str_path = string.Empty;
        List<string> retailers = new List<string>();
        List<string> app_list = new List<string>();
        List<string> subsurb_list = new List<string>();
        List<string> name_list = new List<string>();
        string[] redir_urls = new string[2];
        string[] pdf_links = new string[2];
        private string date_end = string.Empty;

        //Initialize httpclient
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
        //Get Store ID
        public string get_storeId(string post_code)
        {
            string url1 = "https://www.coles.com.au/storelocator/api/getMachedLocalities?urlText=" + post_code + "&lat=22.2&lon=113.55&userLocation=";
            var result1 = client.GetAsync(url1).Result;
            result1.EnsureSuccessStatusCode();
            string strResult = result1.Content.ReadAsStringAsync().Result;
            dynamic jsonresult1 = JsonConvert.DeserializeObject(strResult);
            dynamic json1 = jsonresult1.LocationList;
            string retailerCode = json1[0].reg.Value;
            string store_id = string.Empty;
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000;
            string timestamp = ticks.ToString();
            long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            tick1 /= 10000;
            string timestamp1 = ticks.ToString();
            string callback = "jQuery17205845032764440767_" + timestamp;
            string location_url = "https://webservice.salefinder.com.au/index.php/api/regions/areasearch/?apikey=c0l8sDE5683419EEF6&area=" + retailerCode + "&postcode=" + post_code + "&format=jsonp&callback=" + callback + "&_=" + timestamp1;
            Uri uri = new Uri(location_url);
            var result = client.GetAsync(uri).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            string json_content = Between(strContent + ")", callback + "(", "))");
            dynamic stuff = JsonConvert.DeserializeObject(json_content);
            store_id = stuff.storeId.Value;
            return store_id;
        }

        //Get Location ID
        public void get_url(string storeId)
        {
            string redirect_url = string.Empty;
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000;
            string timestamp = ticks.ToString();
            long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            tick1 /= 10000;
            string timestamp1 = ticks.ToString();
            string callback = "jQuery17205845032764440767_" + timestamp;
            string reUrl = "https://embed.salefinder.com.au/catalogues/view/148/?format=json&order=oldestfirst&saleGroup=0&locationId="+storeId+"&callback="+callback+"&_="+timestamp1;
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
                redir_urls[0] = saleId;
                redir_urls[1] = saleId;
              
                //Get pdf url
                string pdf_content = stuff.content;
                pdf_content = pdf_content.Replace("\\\"", "\"");
                pdf_content = pdf_content.Replace("\\\"", "");
                pdf_content = pdf_content.Replace("\\/", "/");
                pdf_content = pdf_content.Replace("\\n", string.Empty);
                pdf_content = pdf_content.Replace("\\t", string.Empty);
                pdf_content = pdf_content.Replace("\\r", string.Empty);
                HtmlAgilityPack.HtmlDocument doc1 = new HtmlAgilityPack.HtmlDocument();
                doc1.LoadHtml(pdf_content);
                HtmlNode pdf_node = doc1.DocumentNode.SelectSingleNode(".//a[@class='sf-download-link']");
                if (pdf_node == null)
                {
                    return;
                }
                pdf_links[0] = pdf_node.Attributes["href"].Value;
            }
            else
            {
                String content = stuff.content.Value;
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(content);
                IEnumerable<HtmlNode> nodeDiv = doc.DocumentNode.Descendants("div").Where(node => node.Attributes["class"] != null && node.Attributes["class"].Value == "sale-image-cell");
                IEnumerable<HtmlNode> nodepdf = doc.DocumentNode.Descendants("a").Where(node => node.Attributes["class"] != null && node.Attributes["class"].Value == "sf-download-link");

                if (nodeDiv == null || nodeDiv.LongCount() < 1)
                    return;

                for (int i = 0; i < 2; i++)
                {
                   
                        redir_urls[i] = Between(nodeDiv.ToArray()[i].SelectSingleNode(".//a").Attributes["href"].Value, "saleId=", "&areaName");
                        pdf_links[i] = nodepdf.ToArray()[i].Attributes["href"].Value;

                   
                }
                
            }
            

        }

        //Main scraping fucntion
        public bool coleScrap(string str,string root_dir,string postcode,string file_name) 
        {
            initHttpClient();
            string first_row = "CATALOGUE_START" + ',' + "CATALOGUE_END" + ',' + "RETAILER" + ',' + "Description" + ',' + "Page" + ',' + "Promo Type" + ',' + "Multibuy Qty" + ',' + "Price Per Unit" + ',' + "Price Discount";
            string refer_uri = "https://www.coles.com.au/catalogues-and-specials/view-all-available-catalogues";
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", refer_uri);
            var str_array = new List<string>();
            var page_array = new List<string>(); 
            string retailer = string.Empty;
            string url = string.Empty;
            string url1 = string.Empty;
            string url2 = string.Empty;
            string file_path = string.Empty;
            List<string> url_list = new List<string>();
            string ppname = string.Empty;
            ppname = "started";
            FileWriter writer1 = new FileWriter("error.txt");
            try
            {
                //Get timestamp and callback
                long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
                ticks /= 10000;
                string timestamp = ticks.ToString();
                long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
                tick1 /= 10000;
                string timestamp1 = ticks.ToString();
                string callback = "jQuery172016823580768711177_" + timestamp;
                //
                retailer = str;
                string store_id = get_storeId(postcode);
                get_url(store_id);
                foreach (string saleId in redir_urls)
                {
                    url = "https://embed.salefinder.com.au/catalogue/svgData/" + saleId + "/?format=json&pagetype=catalogue2&retailerId=148&locationId=" + store_id + "&size=518&preview=&callback=" + callback + "&_=" + timestamp1;
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
                    string json_content = Between(strContent, callback + "(", ")");
                    dynamic stuff = JsonConvert.DeserializeObject(json_content);
                    dynamic content = stuff.catalogue;


                    string pdf_url = pdf_links[j];
                    //

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
                            try
                            {
                                dynamic item1 = cat_item.Value;
                                if (cat_item.Name == "imagefile")
                                {
                                    break;
                                }
                                if (cat_item.Name == "firstpage")
                                {
                                    break;
                                }
                                string item_name = item1.itemName;
                                if (item_name == null)
                                {
                                    string row = started_date + ',' + ended_date + ',' + retailer + ',' + "" + ',' + i + ',' + "" + ',' + "" + ',' + "0" + ',' + "0";
                                    writer.WriteRow(row);
                                    break;
                                }
                                string item_id = item1.itemId;
                                string page = i.ToString();
                                string itemUrl = "https://embed.salefinder.com.au/item/tooltip/" + item_id + "?callback=" + callback + "&preview=&saleGroup=0&maxWidth=450&maxHeight=460&_=" + timestamp1;
                                var item_content = client.GetAsync(new Uri(itemUrl)).Result;
                                result.EnsureSuccessStatusCode();
                                string item_result = item_content.Content.ReadAsStringAsync().Result;
                                if (item_result.Contains("Selected cases may not be available in all stores"))
                                {
                                    break;

                                }
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
                                string product_date = string.Empty;
                                string product_start = string.Empty;
                                string product_end = string.Empty;
                                string promo_type = string.Empty;
                                string image_url = string.Empty;
                                string multibuy_Qty = string.Empty;
                                string start_Date = string.Empty;
                                item_name = HttpUtility.HtmlDecode(item_name);
                                item_name = filterDescription(item_name);
                                product_des = item_name.Trim();
                                ppname = product_des;
                                if (product_des.Contains("NEW"))
                                {
                                    product_des = product_des.Replace("NEW", "").TrimStart();
                                    promo_type = "New Line";

                                }
                                image_url = nodeImg.SelectSingleNode(".//img").Attributes["src"].Value;
                                image_url = image_url.Replace("/300x300", "");
                                HtmlNode nodeDes = nodeDiv.ToArray()[0].SelectSingleNode(".//div[@id='sf-item-tooltip-details-container']");
                                if (nodeDes != null)
                                {

                                    product_date = nodeDes.SelectSingleNode(".//div[@class='sf-tooltip-saledates']").InnerText;
                                    product_date = Between(product_date + ")", "Offer valid", ")");
                                    string[] str_arr = product_date.Split('-');
                                    string[] str_start = str_arr[0].Trim().Split(' ');
                                    string[] str_end = str_arr[1].Trim().Split(' ');
                                    string first_month = get_month(str_start[2]);
                                    string first_day = str_start[1];
                                    string second_day = str_end[1];
                                    string second_year = str_end[3];
                                    string second_month = get_month(str_end[2]);
                                    float qty = 0;
                                    if (first_day.Length == 1)
                                    {
                                        first_day = "0" + first_day;
                                    }
                                    if (second_day.Length == 1)
                                    {
                                        second_day = "0" + second_day;
                                    }
                                    product_start = first_day + "/" + first_month + "/" + second_year;
                                    started_date = product_start;
                                    product_end = second_day + "/" + second_month + "/" + second_year;
                                    ended_date = product_end;
                                    start_Date = second_year + "_" + first_month + "_" + first_day;
                                    string path = file_name.Replace("_YYYY_MM_DD","");
                                    file_name = path;
                                    file_name = file_name.Replace(" ", "");
                                    createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                    downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                    file_path = file_name + "_" + start_Date + ".csv";
                                    str_path = file_path;
                                    writer = new FileWriter(root_dir + "//" + file_path);
                                    writer.WriteData(first_row, root_dir + "//" + file_path);
                                    downloadPdf(pdf_url, root_dir + "//" + file_name + "_" + start_Date + ".pdf");

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
                                        //  product_disprice = Between("/" + product_disprice, "/", ".");
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        if (product_regpirce != "")
                                        {
                                            product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                        }

                                    }
                                    else if (multi.Contains("both for"))
                                    {
                                        qty = 1;
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        if (product_regpirce != "")
                                        {
                                            product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                        }

                                    }
                                    else if (multi.Contains("Both for"))
                                    {
                                        qty = 1;
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        if (product_regpirce != "")
                                        {
                                            product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                        }

                                    }
                                    else if (multi.Contains("All") && multi.Contains("for"))
                                    {
                                        multibuy_Qty = Between(multi, "All", "for").Trim();
                                        qty = float.Parse(multibuy_Qty);
                                        // product_disprice = Between("/" + product_disprice, "/", ".");
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        if (product_regpirce != "")
                                        {
                                            product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                        }

                                    }
                                    else if (multi.Contains("for"))
                                    {
                                        if (multi.Contains("GB"))
                                        {

                                        }
                                        else
                                        {
                                            multibuy_Qty = Between("Any" + multi, "Any", "for").Trim();
                                            qty = float.Parse(multibuy_Qty);
                                            // product_disprice = Between("/" + product_disprice, "/", ".");
                                            product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                            if (product_regpirce != "")
                                            {
                                                product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");

                                            }

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
                                        if (nodeDes.SelectSingleNode(".//span[@class='sf-regoption']").InnerText.Contains("Was") && (!nodeDes.SelectSingleNode(".//span[@class='sf-regoption']").InnerText.Contains("Save")))
                                        {
                                            reg_price = reg_price - dis_price;
                                            if (reg_price.ToString().Contains("-"))
                                            {
                                                product_regpirce = "0";
                                            }
                                            else
                                            {
                                                product_regpirce = reg_price.ToString();
                                            }
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

                                            if (page_array[key] == i.ToString())
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
                            catch (Exception e) 
                            {
                                writer1.WriteRow(ppname);
                                continue;

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
         public void downloadFile(string dir, string img_name, string link)
        {
            try
            {
            
                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                //responseMessage.EnsureSuccessStatusCode();
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }
                byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                if (contents == null || contents.Length < 1)
                    return;
                string fileName = dir + "\\" + img_name + ".jpg";
                System.IO.File.WriteAllBytes(fileName, contents);
            }
            catch (Exception e)
            {
            }
        }

        //Get string between two strings
        public string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            return FinalString;
        }

        //Get Month number
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
            string val= string.Empty;
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

        //Get download pdf file
        public void downloadPdf(string url,string dir)
        {
            if(!File.Exists(dir))
            {
                web = new WebClient();
                web.DownloadFile(url, dir);   
            }
            
        }
    }
}
