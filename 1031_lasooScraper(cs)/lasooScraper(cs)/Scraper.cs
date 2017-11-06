using HtmlAgilityPack;
using lasooScraper_cs_.NewFolder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace lasooScraper_cs_
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
        List<string>cat_ids =new List<string>();
        private string date_end = string.Empty;
        private string store_id = string.Empty;
        private string store_name = string.Empty;
        List<string> imageUrls = new List<string>();
        List<string> startDate = new List<string>();
        List<string> endDate = new List<string>();
        private string domain = string.Empty;

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
        private void get_storeId(string post_code, string referUrl)
        {

            client.DefaultRequestHeaders.Referrer = null;
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", referUrl);
            string location_url = "http://" + domain + "/remoteprocess/getLocations.html?featureClass=P&style=full&name_startsWith=" + post_code;
            Uri uri = new Uri(location_url);
            var result = client.GetAsync(uri).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            dynamic stuff = JsonConvert.DeserializeObject(strContent);
            dynamic result_list = stuff.geonames;
            if (result_list.Count == 0)
            {
                store_name = post_code;
            }
            else
            {
                store_name = result_list[0].displayName.Value;

            }

        }

        //get_locationID
        private void get_cateID(string Url)
        {
            mainUrl_list.Clear();
            cat_ids.Clear();
            string followUrl = Url.Replace("http://www.lasoo.com.au", "");
            string url = "http://" + domain + "/blankpage.html?type=setlocation&location=" + store_name + "&following=" + followUrl;
            var url1 = new Uri(url);
            var result = client.GetAsync(url1).Result;
            result.EnsureSuccessStatusCode();
            string resultStr = result.Content.ReadAsStringAsync().Result;

            client.DefaultRequestHeaders.Referrer = null;
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", url);
            var resultContent = client.GetAsync(Url).Result;
            resultContent.EnsureSuccessStatusCode();
            string content = resultContent.Content.ReadAsStringAsync().Result;
            if (content.Contains("Your search did not match any shopping results."))
            {
                return;
            }
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);
            IEnumerable<HtmlNode> divcats = doc.DocumentNode.SelectNodes(".//div[@class='catalogue-thumbnail']");
            if (domain.Contains("nz"))
            {
                HtmlNode atag = divcats.ToArray()[1].SelectSingleNode(".//a");
                string jsonstr = atag.Attributes["data"].Value;
                dynamic json = JsonConvert.DeserializeObject(jsonstr);
                cat_ids.Add(json.objectid.Value);
            }
            else
            {
                foreach (HtmlNode node in divcats)
                {
                    HtmlNode atag = node.SelectSingleNode(".//a");
                    string jsonstr = atag.Attributes["data"].Value;
                    dynamic json = JsonConvert.DeserializeObject(jsonstr);
                    cat_ids.Add(json.objectid.Value);

                }

            }



        }
        private string regStr(string content, string pattern, string replaceStr)
        {
            string result = string.Empty;
            Regex rgx = new Regex(pattern);
            result = rgx.Replace(content, replaceStr);

            return result;

        }
        private string get_pdfUrl(string id)
        {
            client.DefaultRequestHeaders.Referrer = null;
            string url = string.Empty;
            if (domain.Contains("nz"))
            {
                url = "http://lasoo.com.au/api/catalogue;sver=j8wmdm7cszp9rt2017291;domain=" + domain + ";catalogueids=" + id + "?jsonp=mf45086582201";

            }
            else if (domain.Contains("au"))
            {

                url = "http://" + domain + "/api/catalogue;sver=j8b2bk54szp9rt2017277;domain=" + domain + ";catalogueids=" + id + "?jsonp=mf45086582201";

            }

            var result = client.GetAsync(url).Result;
            result.EnsureSuccessStatusCode();
            string strContent = result.Content.ReadAsStringAsync().Result;
            strContent = Between(strContent, "mf45086582201(", ");");
            dynamic jsonContent = JsonConvert.DeserializeObject(strContent);
            string pdfUrl = string.Empty;
            pdfUrl = jsonContent.catalogues[0].pdf.Value;
            string startDay = jsonContent.catalogues[0].startDate.Value;
            startDay = startDay.Replace("UTC", "");
            DateTime dateNow;
            dateNow = Convert.ToDateTime(startDay);
            dateNow = dateNow.AddHours(15);
            startDay = dateNow.ToString("yyyy/MM/dd");
            string[] str_arr = startDay.Split(' ');
            startDay = str_arr[0];
            DateTime endTime;
            string endDay = jsonContent.catalogues[0].expiryDate.Value;
            endDay = endDay.Replace("UTC", "");
            endTime = Convert.ToDateTime(endDay);
            endTime = endTime.AddHours(15);
            endDay = endTime.ToString("yyyy/MM/dd");
            startDate.Add(startDay);
            endDate.Add(endDay);
            return pdfUrl;

        }
        /* private void CreatePDF()
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
          }*/



        //Main scraping
        public bool lasooScrape(string str, string root_dir, string storeUrl, string postcode, string file_name)
        {
            try
            {
                CookieContainer container = null;
                initHttpClient(container);
                string first_row = "CATALOGUE_START" + ',' + "CATALOGUE_END" + ',' + "RETAILER" + ',' + "Description" + ',' + "Page" + ',' + "Promo Type" + ',' + "Multibuy Qty" + ',' + "Price Per Unit" + ',' + "Price Discount";

                var str_array = new List<string>();
                var page_array = new List<string>();
                string retailer = string.Empty;
                startDate.Clear();
                endDate.Clear();
                domain = Between(storeUrl, "http://", "/retailer/");
                List<string> url_list = new List<string>();
                var firstResult = client.GetAsync(storeUrl).Result;
                firstResult.EnsureSuccessStatusCode();
                string firstContent = firstResult.Content.ReadAsStringAsync().Result;



                //get real scraping url
                retailer = str;
                get_storeId(postcode, storeUrl);
                get_cateID(storeUrl);
                List<string> pdf_list = new List<string>();
                string mainUrl = string.Empty;
                foreach (string id in cat_ids)
                {
                    pdf_list.Add(get_pdfUrl(id));
                    if (domain.Contains("nz"))
                    {
                        mainUrl = "http://lasoo.com.au/api/cataloguepage;sver=j8wmdm7cszp9rt2017291;domain=" + domain + ";catalogueid=" + id + ";allpages=1?jsonp=mf75847969961";
                    }
                    else if (domain.Contains("au"))
                    {
                        mainUrl = "http://" + domain + "/api/cataloguepage;sver=j8b2bk54szp9rt2017277;domain=" + domain + ";catalogueid=" + id + ";allpages=1?jsonp=mf75847969961";
                    }
                    mainUrl_list.Add(mainUrl);
                }

                for (int j = 0; j < mainUrl_list.Count; j++)
                {
                    string product_date = string.Empty;
                    string product_start = string.Empty;
                    string product_end = string.Empty;
                    string start_Date = string.Empty;
                    imageUrls.Clear();
                    client.DefaultRequestHeaders.Referrer = null;
                    Uri uri = new Uri(mainUrl_list[j]);
                    var result = client.GetAsync(uri).Result;
                    result.EnsureSuccessStatusCode();
                    string strContent = result.Content.ReadAsStringAsync().Result;
                    strContent = Between(strContent, "mf75847969961(", ");");
                    string[] str_start = null;
                    string[] str_end = null;
                    if (startDate[j].Contains('/'))
                    {
                        str_start = startDate[j].Trim().Split('/');

                    }
                    else if (startDate[j].Contains('-'))
                    {
                        str_start = startDate[j].Trim().Split('-');
                    }

                    if (endDate[j].Contains('/'))
                    {
                        str_end = endDate[j].Trim().Split('/');
                    }
                    else if (endDate[j].Contains('-'))
                    {
                        str_end = endDate[j].Trim().Split('-');
                    }
                    //string first_month = get_month(str_start[1]);
                    //string second_month = get_month(str_end[1]);
                    product_start = str_start[0] + "/" + str_start[1] + "/" + str_start[2];
                    product_end = str_end[0] + "/" + str_end[1] + "/" + str_end[2];
                    start_Date = str_start[0] + "_" + str_start[1] + "_" + str_start[2];
                    //
                    page_array.Clear();
                    str_array.Clear();
                    file_path = "";


                    dynamic content = JsonConvert.DeserializeObject(strContent);
                    List<jsonClass> jsonArr = new List<jsonClass>();
                    jsonArr = JsonConvert.DeserializeObject<List<jsonClass>>(strContent);


                    for (int i = 0; i < jsonArr.Count; i++)
                    {
                        List<offer> offerList = jsonArr[i].offers;
                        List<offer> newOffers = new List<offer>();
                        foreach (offer item in offerList)
                        {
                            if (item.offerimage != null)
                            {
                                string path = item.offerimage.path;
                                path = lastBetween(path, "/", ".jpg");
                                item.offerimage.path = path;
                                newOffers.Add(item);
                            }

                        }

                        string pdfimage = string.Empty;
                        string url1 = string.Empty;
                        List<offer> content2 = new List<offer>();
                        if (domain.Contains("nz"))
                        {
                            content2 = newOffers.OrderBy(o => o.offerimage.path).ToList();

                        }
                        else
                        {
                            content2 = newOffers;
                        }


                        foreach (offer cat_item in content2)
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

                            string item_id = cat_item.id;
                            string item_name = cat_item.title;
                            string itemUrl = string.Empty;
                            string page = string.Empty;
                            if (domain.Contains("nz"))
                            {
                                itemUrl = "http://lasoo.com.au/api/offer;sver=j8wmdm7cszp9rt2017291;domain=" + domain + ";id=" + item_id + "?jsonp=mf80360516461";
                            }
                            else if (domain.Contains("au"))
                            {
                                itemUrl = "http://" + domain + "/api/offer;sver=j8b2bk54szp9rt2017277;domain=" + domain + ";id=" + item_id + "?jsonp=mf80360516461";
                            }
                            var item_content = client.GetAsync(new Uri(itemUrl)).Result;
                            result.EnsureSuccessStatusCode();
                            string item_result = item_content.Content.ReadAsStringAsync().Result;
                            if (item_result.Contains("ERROR NOTIFICATION"))
                            {
                                continue;
                            }
                            if (item_result.Contains("mf80360516461([]);"))
                            {
                                continue;
                            }
                            item_result = Between(item_result, "mf80360516461([", "]);");

                            //item_result = item_result.Replace("[","\"[");
                            // item_result = item_result.Replace("]", "]\"");
                            dynamic json_item = JObject.Parse(item_result);
                            string product_des = string.Empty;
                            string product_disprice = string.Empty;
                            string product_regpirce = string.Empty;

                            string promo_type = string.Empty;
                            string image_url = string.Empty;
                            string multibuy_Qty = string.Empty;
                            float qty = 0;
                            item_name = HttpUtility.HtmlDecode(item_name);
                            page = json_item.pageNo;
                            item_name = filterDescription(item_name);
                            item_name = item_name.Trim();
                            product_des = item_name;
                            //image_url = nodeImg.SelectSingleNode(".//img").Attributes["src"].Value;

                            if (json_item.offerimage.path == null)
                            {

                            }
                            else
                            {
                                image_url = json_item.offerimage.path;
                            }

                            //get product informations

                            if (j == 0)
                            {
                                string path = file_name.Replace("_YYYY_MM_DD", "");
                                file_name = path;
                                file_name = file_name.Replace(" ", "");
                                file_path = file_name + "_" + start_Date + ".csv";
                                createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                downloadPdf(pdf_list[j], root_dir + "//" + file_name + "_" + start_Date + ".pdf");
                                writer = new FileWriter(root_dir + "//" + file_path);
                                writer.WriteData(first_row, root_dir + "//" + file_path);
                            }
                            else
                            {
                                string path = file_name.Replace("_YYYY_MM_DD", "");
                                file_name = path;
                                file_name = file_name.Replace(" ", "");
                                if (startDate[j].Contains(startDate[j - 1]))
                                {
                                    file_path = file_name + "_" + start_Date + "-" + j.ToString() + ".csv";
                                    createDrectory(root_dir + "\\" + file_name + "_" + start_Date + "-" + j.ToString());
                                    downloadFile(root_dir + "\\" + file_name + "_" + start_Date + "-" + j.ToString(), product_des, image_url);
                                    downloadPdf(pdf_list[j], root_dir + "//" + file_name + "_" + start_Date + "-" + j.ToString() + ".pdf");

                                }
                                else
                                {
                                    file_path = file_name + "_" + start_Date + ".csv";
                                    createDrectory(root_dir + "\\" + file_name + "_" + start_Date);
                                    downloadFile(root_dir + "\\" + file_name + "_" + start_Date, product_des, image_url);
                                    downloadPdf(pdf_list[j], root_dir + "//" + file_name + "_" + start_Date + ".pdf");

                                }

                                writer = new FileWriter(root_dir + "//" + file_path);
                                writer.WriteData(first_row, root_dir + "//" + file_path);

                            }

                            if (json_item.priceValue == null)
                            {

                            }
                            else
                            {
                                product_disprice = json_item.priceValue;
                                product_disprice = product_disprice.Replace(":", "");
                                if (domain.Contains("nz"))
                                {
                                    if (json_item.price != null)
                                    {

                                        string temp_price = json_item.price.Value;
                                        temp_price = temp_price.ToLower().Trim();
                                        if (!temp_price.Contains("up to"))
                                        {
                                            temp_price = temp_price.Replace("each", "");
                                            temp_price = temp_price.Replace("ea", "");
                                            temp_price = temp_price.Replace("$", "");
                                            temp_price = temp_price.Replace("save", "");
                                            temp_price = temp_price.Replace("kg", "");
                                            temp_price = temp_price.Replace("pack", "");
                                            temp_price = temp_price.Replace("bag", "");
                                            temp_price = temp_price.Replace("bunch", "");
                                            temp_price = temp_price.Replace("pk", "");
                                            temp_price = temp_price.Replace("box", "");
                                            temp_price = temp_price.Replace("punnet", "");
                                            if (temp_price.Contains("for"))
                                            {
                                                temp_price = Between(temp_price + "xx", "for", "xx");
                                            }
                                            else if (temp_price.Contains("per"))
                                            {
                                                temp_price = Between("xx" + temp_price, "xx", "per");

                                            }
                                            if (temp_price.Contains(product_disprice))
                                            {
                                                product_disprice = temp_price;
                                            }

                                        }



                                    }

                                }

                            }

                            if (json_item.saving == null)
                            {
                            }
                            else
                            {
                                product_regpirce = json_item.saving;
                                if (product_regpirce != "")
                                {
                                    if (product_regpirce.Contains(""))
                                    {
                                        if (product_regpirce.Contains("/"))
                                        {
                                            product_regpirce = Between(product_regpirce, "$", "/");
                                        }

                                        product_regpirce = product_regpirce.Replace("SAVE FROM", "");
                                        product_regpirce = product_regpirce.Replace("Save $", "");
                                        product_regpirce = product_regpirce.Replace("OFF RRP", "");
                                        product_regpirce = product_regpirce.Replace("off RRP", "");
                                        product_regpirce = product_regpirce.Replace("Off RRP", "");
                                        product_regpirce = product_regpirce.Replace("EA", "");
                                        product_regpirce = product_regpirce.Replace("SAVE $", "");
                                        product_regpirce = product_regpirce.Replace("Save up to $", "");
                                        product_regpirce = product_regpirce.Replace("Save UP TO $", "");
                                        product_regpirce = product_regpirce.Replace("SAVE UP TO $", "");
                                        product_regpirce = product_regpirce.Replace("Save Up To $", "");
                                        product_regpirce = product_regpirce.Replace("Save up to", "");
                                        product_regpirce = product_regpirce.Replace("See In-Store", "");
                                        product_regpirce = product_regpirce.Replace("Save 50%", "");
                                        product_regpirce = product_regpirce.Replace("ea", "");
                                        product_regpirce = product_regpirce.Replace("each", "");
                                        product_regpirce = product_regpirce.Replace("EACH", "");
                                        product_regpirce = product_regpirce.Replace("Save from", "");
                                        product_regpirce = product_regpirce.Replace("$", "");
                                        product_regpirce = product_regpirce.Replace("Save", "");
                                        product_regpirce = product_regpirce.Replace("¢", "");
                                        if (product_regpirce[product_regpirce.Length - 1] == '.')
                                        {
                                            product_regpirce = product_regpirce.Remove(product_regpirce.Length - 1);
                                        }
                                        if (product_regpirce.Contains("%"))
                                        {
                                            product_regpirce = product_regpirce.Replace(product_regpirce, "");
                                        }
                                        product_regpirce = product_regpirce.Trim();


                                    }

                                }

                            }


                            string multi = string.Empty;
                            if (json_item.price == null)
                            {

                            }
                            else
                            {

                                multi = json_item.price;
                                multi = multi.ToLower();

                            }
                            if (multi.Contains("buy"))
                            {
                            }
                            if (multi.Contains("any") && multi.Contains("for"))
                            {
                                multi = convertNum(multi);
                                multibuy_Qty = Between(multi, "any", "for").Trim();
                                qty = float.Parse(multibuy_Qty);
                                // product_disprice = Between("/" + product_disprice, "/", ".");
                                product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                if (product_regpirce == "")
                                {

                                }
                                else
                                {
                                    product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                }
                            }
                            else if (multi.Contains("all") && multi.Contains("for"))
                            {
                                multi = convertNum(multi);
                                multibuy_Qty = Between(multi, "all", "for").Trim();
                                qty = float.Parse(multibuy_Qty);
                                // product_disprice = Between("/" + product_disprice, "/", ".");
                                product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");
                                if (product_regpirce == "")
                                {
                                }
                                else
                                {
                                    product_regpirce = (float.Parse(product_regpirce) / qty).ToString("N2");
                                }

                            }
                            else if (multi.Contains("both for"))
                            {

                            }
                            else if (multi.Contains("buy") && multi.Contains("for"))
                            {
                                multi = convertNum(multi);
                                multibuy_Qty = Between(multi, "buy", "for").Trim();
                                qty = float.Parse(multibuy_Qty);
                                //product_disprice = Between("/" + product_disprice, "/", ".");
                                if ((float.Parse(product_disprice) / qty).ToString("N2") == "0.50")
                                {
                                    product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");

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
                            else if (multi.Contains("for"))
                            {

                                multi = convertNum(multi);
                                multibuy_Qty = Between("any" + multi, "any", "for").Trim();
                                qty = float.Parse(multibuy_Qty);
                                //product_disprice = Between("/" + product_disprice, "/", ".");
                                if ((float.Parse(product_disprice) / qty).ToString("N2") == "0.50")
                                {
                                    product_disprice = (float.Parse(product_disprice) / qty).ToString("N2");

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

                            if (qty > 1)
                            {
                                promo_type = "Multibuy";
                            }
                            else if (reg_price >= dis_price && reg_price > 0 && dis_price > 0)
                            {
                                if (promo_type != "New Line")
                                {
                                    //promo_type = "Half Price";

                                }

                            }
                            else
                            {
                                if (promo_type != "New Line")
                                {
                                    promo_type = "Price Reduction";
                                }
                            }
                            if (json_item.description == null)
                            {
                            }
                            else
                            {
                                string des = json_item.description;
                                des = des = des.ToLower();
                                if (des.Contains("every day"))
                                {
                                    promo_type = "EDLP";

                                }
                                else if (des.Contains("prices dropped"))
                                {
                                    promo_type = "Price Drop";

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
                            string row = string.Empty;
                            if (product_disprice == "0")
                            {
                                row = product_start + ',' + product_end + ',' + retailer + ',' + product_des + ',' + page + ',' + promo_type + ',' + multibuy_Qty + ',' + "0" + ',' + "0";

                            }
                            else
                            {
                                row = product_start + ',' + product_end + ',' + retailer + ',' + product_des + ',' + page + ',' + promo_type + ',' + multibuy_Qty + ',' + product_disprice + ',' + product_regpirce;


                            }

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
                    if (j == 0)
                    {
                        pdf_falePath = root_dir + "//" + file_name + "_" + start_Date + ".pdf";


                    }
                    else
                    {
                        pdf_falePath = root_dir + "//" + file_name + "_" + start_Date + "-1" + ".pdf";

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
        private string lastBetween(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.LastIndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.LastIndexOf(LastString);
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

        //Read csv file
        private void ReadCsv(string filepath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filepath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        retailers.Add(values[0]);
                        app_list.Add(values[3]);
                        subsurb_list.Add(values[4]);
                        name_list.Add(values[2]);
                        Url_list.Add(values[1]);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
