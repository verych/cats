using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CatsDBManager
{
    public class GoogleCustomSearch
    {

        //const string apiKey = "AIzaSyB4yNpGGJ4sLJ5BFJ5rVwj3QeBUckYTIJM";
        const string apiKey = "AIzaSyCI8NwjQhUObtyF9XdbsDgxZqhmYAFHxC4";
        //const string apiKey = "AIzaSyBeby5dSnCaL5Sda958FS1k05GYPe3qTZw";
        //const string apiKey = "AIzaSyDH_Guccn05dAiCblnzawOyTPUUbaTyhO8";
        //const string apiKey = "AIzaSyBOAKCOWhjek6HkuJPV26eTiXr5oWYc77I"; 
        const string searchEngineId = "008850977117535080700:fpopnpyaip4";

        public List<Image> GetImages(string term, int pageCount = 10)
        {
            List<Image> result = new List<Image>();
            var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
            var listRequest = customSearchService.Cse.List(term);
            listRequest.Cx = searchEngineId;
            listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;

            IList<Result> paging = new List<Result>();
            var count = 0;
            while (count < pageCount && paging != null)
            {
                listRequest.Start = count * 10 + 1;
                paging = listRequest.Execute().Items;
                if (paging != null)
                    foreach (var item in paging)
                    {
                        var img = GetImageByURL(item.Link);
                        if (img != null)
                        {
                            result.Add(img);
                        }
                    }
                count++;
            }
            return result;
        }

        private Image GetImageByURL(string link)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    byte[] data = webClient.DownloadData(link);
                    MemoryStream mem = new MemoryStream(data);
                    var itemImage = Image.FromStream(mem);
                    return itemImage;
                 }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
