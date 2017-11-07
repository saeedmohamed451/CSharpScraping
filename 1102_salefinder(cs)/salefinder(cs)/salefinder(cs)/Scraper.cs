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
using System.Web.UI;
using iTextSharp;
using System.IO;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace salefinder_cs_
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
        private string file_path = string.Empty;
        private string pdf_falePath = string.Empty;
        List<string> retailers = new List<string>();
        List<string> app_list = new List<string>();
        List<string> subsurb_list = new List<string>();
        List<string> name_list = new List<string>();
        List<string> Url_list = new List<string>();
        List<string> mainUrl_list = new List<string>();
        string []redir_urls =new string[2];
        private string date_end = string.Empty;
        private string store_id = string.Empty;
        private string store_name = string.Empty;
        List<string> imageUrls = new List<string>();


   
        //Initialize httpclient
        private void initHttpClient()
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
        private void get_storeId(string post_code,string referUrl) 
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000;
            string timestamp = ticks.ToString();
            long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            tick1 /= 10000;
            string timestamp1 = ticks.ToString();
            string callback = "jQuery17205845032764440767_" + timestamp;
            client.DefaultRequestHeaders.Referrer = null;
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", referUrl);
            string location_url = "https://salefinder.com.au/ajax/locationsearch?callback="+callback+"&query="+post_code+"&_="+timestamp1;
            Uri uri = new Uri(location_url);
            var result = client.GetAsync(uri).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            string json_content = Between(strContent+")", callback + "(", "))");
            dynamic stuff = JsonConvert.DeserializeObject(json_content);
            dynamic result_list = stuff.suggestions;
            store_id = result_list[0].data.Value;
            store_name = result_list[0].value.Value;
            
        }

        //get_locationID
        private void get_url(string Url) 
        {
            mainUrl_list.Clear();
            var url = new Uri(Url);
            string postCode = store_id;
            container.Add(url, new Cookie("postcodeId",store_id));
            string regionName = WebUtility.UrlEncode(store_name);
            container.Add(url, new Cookie("regionName", regionName));
            var result = client.GetAsync(url).Result;
            result.EnsureSuccessStatusCode();
            string resultStr = result.Content.ReadAsStringAsync().Result;
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(resultStr);
            IEnumerable<HtmlNode> nodeDivs = doc.DocumentNode.SelectNodes(".//a[@class='catalogue-image']");
            if (nodeDivs == null || nodeDivs.LongCount() < 1)
            {
                return;
            }
            foreach (HtmlNode node in nodeDivs) 
            {
                mainUrl_list.Add(node.Attributes["href"].Value);
            }

        }
        private string regStr(string content,string pattern,string replaceStr) 
        {
            string result = string.Empty;
            Regex rgx = new Regex(pattern);
            result = rgx.Replace(content, replaceStr);

            return result;

        }
       private void CreatePDF()
        {
            if (imageUrls.Count() >= 1)
            {
                Document document = new Document(PageSize.LETTER);
                try
                {

                    // step 2:
                    // we create a writer that listens to the document
                    // and directs a PDF-stream to a file

                    PdfWriter.GetInstance(document, new FileStream(pdf_falePath, FileMode.Create));

                    // step 3: we open the document
                    document.Open();

                    foreach (var image in imageUrls)
                    {
                        iTextSharp.text.Image pic = iTextSharp.text.Image.GetInstance(image);
                        float percent = 0.0f;
                        float width = 0.0f;
                        width = 580 / pic.Width;
                        percent = 750 / pic.Height;
                        //pic.ScalePercent(percent * 100);
                        //pic.ScalePercent(width * 100);
                        //pic.ScaleToFit(width, percent);
                        pic.SetAbsolutePosition(10,0);
                        pic.ScaleToFit(document.PageSize.Width, document.PageSize.Height);
                       // pic.BorderWidth = 0f;
                       // pic.SpacingAfter = 1f;
                       // pic.SpacingBefore = 1f;
                        document.Add(pic);
                        document.NewPage();
                    }
                }
                catch (DocumentException de)
                {
                    Console.Error.WriteLine(de.Message);
                }
                catch (IOException ioe)
                {
                    Console.Error.WriteLine(ioe.Message);
                }

                // step 5: we close the document
                document.Close();
            }
        }

     

        //Main scraping
        public bool saleScrap(string str,string root_dir,string storeUrl,string postcode,string file_name) 
        {
                initHttpClient();
                try
                {
                    string first_row = "CATALOGUE_START" + ',' + "CATALOGUE_END" + ',' + "RETAILER" + ',' + "Description" + ',' + "Page" + ',' + "Promo Type" + ',' + "Multibuy Qty" + ',' + "Price Per Unit" + ',' + "Price Discount";

                    var str_array = new List<string>();
                    var page_array = new List<string>();
                    string retailer = string.Empty;
                    List<string> url_list = new List<string>();
                    var firstResult = client.GetAsync(storeUrl).Result;
                    firstResult.EnsureSuccessStatusCode();
                    string firstContent = firstResult.Content.ReadAsStringAsync().Result;



                    //get real scraping url
                    retailer = str;
                    get_storeId(postcode, storeUrl);
                    get_url(storeUrl);
                    List<string> preDate = new List<string>();
                    for (int j = 0; j < mainUrl_list.Count; j++)
                    {
                        string product_date = string.Empty;
                        string product_start = string.Empty;
                        string product_end = string.Empty;
                        string start_Date = string.Empty;
                        imageUrls.Clear();

                        if (mainUrl_list[j].Contains("list"))
                        {
                            mainUrl_list[j] = mainUrl_list[j].Replace("list", "catalogue2");

                        }
                        Uri uri = new Uri("https://salefinder.com.au" + mainUrl_list[j]);
                        var result = client.GetAsync(uri).Result;
                        result.EnsureSuccessStatusCode();
                        string strContent = result.Content.ReadAsStringAsync().Result;

                        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(strContent);
                        HtmlNode dateNode = doc.DocumentNode.SelectSingleNode(".//span[@class='sf-catalogue-dates']");
                        product_date = dateNode.InnerText;
                        product_date = Between(product_date + "xx", "Offer valid", "xx");
                        string[] str_arr = product_date.Split('-');
                        string[] str_start = str_arr[0].Trim().Split(' ');
                        string[] str_end = str_arr[1].Trim().Split(' ');
                        string first_month = get_month(str_start[2]);
                        string first_day = str_start[1];
                        string second_day = str_end[1];
                        string second_year = str_end[3];
                        if (first_day.Length == 1)
                        {
                            first_day = "0" + first_day;
                        }
                        if (second_day.Length == 1)
                        {
                            second_day = "0" + second_day;
                        }
                        string second_month = get_month(str_end[2]);
                        product_start = first_day + "/" + first_month + "/" + second_year;
                        product_end = second_day + "/" + second_month + "/" + second_year;
                        start_Date = second_year + "_" + first_month + "_" + first_day;
                        preDate.Add(start_Date);

                        string json_content = Between(strContent, "var Salefinder =", "function trackCataloguePageView");
                        json_content = json_content.Replace("\n\t", "");
                        json_content = json_content.Replace("\t", "");
                        json_content = json_content.Replace("\n", "");
                        string jsonContent = json_content.Substring(0, json_content.Length - 1);
                        json_content = regStr(jsonContent, "data", "\"data\"");
                        json_content = regStr(json_content, "saleId", "\"saleId\"");
                        json_content = regStr(json_content, "view", "\"view\"");
                        json_content = regStr(json_content, "'catalogue2'", "\"catalogue2\"");
                        json_content = regStr(json_content, "page", "\"page\"");
                        json_content = regStr(json_content, "cdn", "\"cdn\"");
                        json_content = regStr(json_content, "'https://d2g5na3xotdfpc.cloudfront.net'", "\"https://d2g5na3xotdfpc.cloudfront.net\"");
                        json_content = regStr(json_content, "carouselList", "\"carouselList\"");
                        json_content = regStr(json_content, "saleYoutubeId", "\"saleYoutubeId\"");
                        json_content = regStr(json_content, "''", "\"\"");
                        json_content = regStr(json_content, "\"page\":", "\"pagg\":");
                        json_content = regStr(json_content, "re\"view\"", "review");
                        json_content = regStr(json_content, "Re\"view\"", "Review");
                        if (json_content.Contains("\"page\""))
                        {
                            json_content = regStr(json_content, "\"page\"", "\\\"page\\\"");

                        }
                        dynamic stuff = JsonConvert.DeserializeObject(json_content);
                        dynamic content = stuff.data.carouselList;
                        //
                        page_array.Clear();
                        str_array.Clear();
                        file_path = "";
                        string page = string.Empty;

                        for (int i = 0; i < content.Count; i++)
                        {
                            string pdfimage = string.Empty;
                            string url1 = string.Empty;
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
                                string pdf_image = string.Empty;
                                if (cat_item.Name.Contains("imagefile"))
                                {
                                    pdfimage = cat_item.Value.Value;
                                    url1 = "https://d2g5na3xotdfpc.cloudfront.net/images/salepages/ipad/" + pdfimage;

                                    file_name = file_name.Replace("_YYYY_MM_DD", "");
                                    file_name = file_name.Replace(" ", "");
                                    string pdfdir = string.Empty;
                                    if (j == 0)
                                    {
                                        pdfdir = root_dir + "\\" + file_name + "_" + start_Date + "_PDF Image";
                                    }
                                    else
                                    {
                                        if (preDate[j] == preDate[j - 1])
                                        {
                                            pdfdir = root_dir + "\\" + file_name + "_" + start_Date + "_PDF Image-1";

                                        }
                                        else
                                        {
                                            pdfdir = root_dir + "\\" + file_name + "_" + start_Date + "_PDF Image";

                                        }
                                    }
                                    createDrectory(pdfdir);
                                    downloadImage(pdfdir, pdfimage, url1);
                                    string imagepath = pdfdir + "\\" + pdfimage;
                                    imageUrls.Add(imagepath);
                                    //pdfwriter = new PDFWriter(pdf_falePath);
                                    //pdfwriter.InsertImage(root_dir + "\\" + file_name + "_" + start_Date + "_PDF Image" + "\\" + pdfimage);


                                }

                                if (cat_item.Value.HasValues == false)
                                {
                                    continue;
                                }
                                if (cat_item.Value.itemId == null)
                                {
                                    continue;
                                }

                                string item_id = cat_item.Value.itemId.Value.ToString();
                                string item_name = cat_item.Value.itemName.Value;
                                string itemUrl = string.Empty;
                                page = (i + 1).ToString();

                                //get timestamp and callback
                                long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
                                ticks /= 10000;
                                string timestamp = ticks.ToString();
                                long tick1 = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
                                tick1 /= 10000;
                                string timestamp1 = ticks.ToString();
                                string callback = "jQuery17205845032764440767_" + timestamp;
                                //
                                itemUrl = "https://salefinder.com.au/ajax/itempopup/" + item_id + "?callback=" + callback + "&preview=&_=" + timestamp1;
                                var item_content = client.GetAsync(new Uri(itemUrl)).Result;
                                result.EnsureSuccessStatusCode();
                                string item_result = item_content.Content.ReadAsStringAsync().Result;
                                item_result = item_result.Replace("\\\"", "");
                                item_result = item_result.Replace("\\/", "/");
                                item_result = item_result.Replace("\\n", string.Empty);
                                item_result = item_result.Replace("\\t", string.Empty);
                                item_result = item_result.Replace("\\r", string.Empty);
                                item_result = Between(item_result, "{\"content\":\"", "\"})");
                                HtmlAgilityPack.HtmlDocument doc1 = new HtmlAgilityPack.HtmlDocument();
                                doc1.LoadHtml(item_result);
                                IEnumerable<HtmlNode> nodeDiv = doc1.DocumentNode.Descendants("div").Where(node => node.Attributes["id"] != null && node.Attributes["id"].Value == "sf-item-tooltip");
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
                                image_url = image_url.Replace("/350x350/", "/");

                                //get product informations
                                HtmlNode nodeDes = nodeDiv.ToArray()[0].SelectSingleNode(".//div[@id='sf-item-tooltip-details-container']");
                                if (nodeDes != null)
                                {
                                    if (j == 0)
                                    {
                                        file_name = file_name.Replace("_YYYY_MM_DD", "");
                                        file_name = file_name.Replace(" ", "");
                                        file_path = file_name + "_" + start_Date + ".csv";
                                        createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                        downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);

                                        writer = new FileWriter(root_dir + "//" + file_path);
                                        writer.WriteData(first_row, root_dir + "//" + file_path);
                                    }
                                    else
                                    {
                                        if (preDate[j] == preDate[j - 1])
                                        {
                                            file_name = file_name.Replace("_YYYY_MM_DD", "");
                                            file_name = file_name.Replace(" ", "");
                                            file_path = file_name + "_" + start_Date + "-" + "1" + ".csv";
                                            createDrectory(root_dir + "\\" + file_name + "_" + start_Date + "-" + "1");
                                            downloadFile(root_dir + "\\" + file_name + "_" + start_Date + "-" + "1", product_des, image_url);
                                            writer = new FileWriter(root_dir + "//" + file_path);
                                            writer.WriteData(first_row, root_dir + "//" + file_path);


                                        }
                                        else
                                        {
                                            file_name = file_name.Replace("_YYYY_MM_DD", "");
                                            file_name = file_name.Replace(" ", "");
                                            file_path = file_name + "_" + start_Date + ".csv";
                                            createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                            downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                            writer = new FileWriter(root_dir + "//" + file_path);
                                            writer.WriteData(first_row, root_dir + "//" + file_path);


                                        }


                                    }

                                    //downloadPdf(pdf_url, root_dir + "//" + file_name + "_" + start_Date + ".pdf");


                                    /*else if (j == 1)
                                    {
                                        string[] arr = file_name.Split('_');
                                        file_name = arr[0];
                                        createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                        downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                        file_path = file_name + "_" + start_Date + ".csv";
                                        file_path = file_path.Trim();
                                        str_path = file_path;
                                        writer = new FileWriter(root_dir + "//" + file_path);
                                        writer.WriteData(first_row, root_dir + "//" + file_path);
                                       // downloadPdf(pdf_url, root_dir + "//" + file_name + "_" + start_Date + ".pdf");

                                    }*/


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
                                    else if (multi.Contains("Buy") && multi.Contains("for"))
                                    {
                                        multibuy_Qty = Between(multi, "Buy", "for").Trim();
                                        qty = float.Parse(multibuy_Qty);
                                        // product_disprice = Between("/" + product_disprice, "/", ".");
                                        product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                        product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
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
                                    /*  if(j == 1)
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

                                          }

                                      }else*/
                                    if (product_disprice == "")
                                    {
                                        product_disprice = "0";
                                    }
                                    if (product_regpirce == "")
                                    {
                                        product_regpirce = "0";
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
                        if (j == 0)
                        {
                            pdf_falePath = root_dir + "//" + file_name + "_" + start_Date + ".pdf";


                        }
                        else
                        {
                            if (preDate[j] == preDate[j - 1])
                            {
                                pdf_falePath = root_dir + "//" + file_name + "_" + start_Date + "-1" + ".pdf";

                            }
                            else
                            {
                                pdf_falePath = root_dir + "//" + file_name + "_" + start_Date + ".pdf";

                            }

                        }
                        CreatePDF();

                    }          
                 
                }
                catch (Exception ex) 
                {
                    return false;

                }
                return true;
            
              
        
        }


        private string filterDescription(string originItemName)
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
        private void createDrectory(string name)
        {

            bool exists = System.IO.Directory.Exists(name);

            if (!exists)
                Directory.CreateDirectory(name);
        }
        //Download Image file
        private void downloadFile(string dir, string img_name, string link)
        {
            try
            {
                if (img_name.Contains(","))
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
                string imgName = dir + "\\" + img_name + ".jpg";
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

                HttpResponseMessage responseMessage = client.GetAsync(link).Result;
                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }
                byte[] contents = responseMessage.Content.ReadAsByteArrayAsync().Result;
                if (contents == null || contents.Length < 1)
                    return;
                string imgName = dir + "\\" + img_name ;
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

        // Download pdf file
        private void downloadPdf(string url,string dir)
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
