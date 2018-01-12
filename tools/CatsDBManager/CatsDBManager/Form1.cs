using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using EyeOpen.Imaging;

namespace CatsDBManager
{
    public partial class Form1 : Form
    {
        public class ListViewImageItem
        {
            public string Name;
            public string Path;
            public ListViewImageItem(string path)
            {
                Path = path;
                Name = System.IO.Path.GetFileName(path);
            }
        }

        private XmlDocument breeds = new XmlDocument();
        private string breedsFile;
        private List<string> ImagesApproved = new List<string>();
        private List<string> ImagesDeclined = new List<string>();
        private string ImagesApprovedPath;
        private string ImagesDeclinedPath;
        private Dictionary<string, List<string>> Duplicates = new Dictionary<string, List<string>>();

        public Form1()
        {
            InitializeComponent();
            InitBreeds();
            buttonLoadBreeds_Click(null, null);
            InitUnlinkedBreeds();
            InitApproveLists();
            InitImages();
            InitDuplicates();
        }

        private void InitDuplicates()
        {
            //
        }

        private void InitApproveLists()
        {
            try
            {
                ImagesApprovedPath = Path.Combine(textBoxRootPath.Text, "images_approved.txt");
                ImagesDeclinedPath = Path.Combine(textBoxRootPath.Text, "images_declined.txt");
                if (!File.Exists(ImagesApprovedPath))
                {
                    File.WriteAllLines(ImagesApprovedPath, ImagesApproved, Encoding.UTF8);
                }
                if (!File.Exists(ImagesDeclinedPath))
                {
                    File.WriteAllLines(ImagesDeclinedPath, ImagesDeclined, Encoding.UTF8);
                }
                ImagesApproved = File.ReadAllLines(ImagesApprovedPath).ToList();
                ImagesDeclined = File.ReadAllLines(ImagesDeclinedPath).ToList();
            }
            catch (Exception ex)
            {
                log(ex.Message);
            }
        }

        private void InitImages()
        {
            var breeds = getBreeds();
            breeds.Sort();
            comboBoxBreed.DataSource = breeds;
            var srcs = getRawSources();
            srcs.Sort();
            srcs.Insert(0, "---All---");
            comboBoxSrc.DataSource = srcs;
            UpdateImages();
        }

        private void UpdateImages()
        {
            var files = GetImages();
            ShowImages(files);
        }

        private void ShowImages(List<string> files)
        {
            var size = new Size(trackBarSize.Value, trackBarSize.Value);
            listViewImages.Clear();
            // create image list and fill it 

            var imageList = new ImageList();
            imageList.ImageSize = size;
            imageList.ColorDepth = ColorDepth.Depth16Bit;
            int index = 0;
            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileName(file);
                bool approved = ImagesApproved.Contains(name);
                bool declined = ImagesDeclined.Contains(name);

                if (checkBoxHideReviewed.Checked && (approved || declined))
                {
                    continue;
                }


                var item = new ListViewImageItem(file);
                Bitmap img;
                try
                {
                    img = new Bitmap(file);
                }
                catch (Exception ex)
                {
                    img = new Bitmap(size.Width, size.Height);
                }
                img = FitImg(img, size);
                imageList.Images.Add(img);
                listViewImages.Items.Add(item.Name, index++);
            }
            listViewImages.LargeImageList = imageList;
            listViewImages.SmallImageList = imageList;
            UpdateImageItems();
        }

        private Bitmap FitImg(Bitmap bmp, Size size)
        {
            var newSize = new Size();
            if (bmp.Width > bmp.Height)
            {
                double k = (double)bmp.Width / size.Width;
                newSize.Width = size.Width;
                newSize.Height = (int)Math.Round(bmp.Height / k);
            }
            else {
                double k = (double)bmp.Height / size.Height;
                newSize.Height = size.Height;
                newSize.Width = (int)Math.Round(bmp.Width / k);
            }
            var resized = new Bitmap(bmp, newSize);
            var blank = new Bitmap(size.Width, size.Height);
            CopyRegionIntoImage(resized, new Rectangle(new Point(0, 0), size), ref blank, new Rectangle(new Point(0, 0), size));
            return blank;
        }

        public void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }

        private List<string> GetImages()
        {
            var result = new List<string>();
            if (comboBoxBreed.SelectedItem == null)
            {
                return result;
            }
            if (comboBoxSrc.SelectedItem == null)
            {
                return result;
            }
            var sources = getRawSources();

            var aliases = GetBreedAliasesByName(comboBoxBreed.SelectedItem.ToString());
            foreach (string alias in aliases)
            {
                foreach (string src in sources)
                {
                    if (src == comboBoxSrc.SelectedItem.ToString() || comboBoxSrc.SelectedIndex == 0)
                    {
                        var path = Path.Combine(src, "breeds", alias);
                        if (Directory.Exists(path))
                        {
                            var files = Directory.GetFiles(path);
                            result.AddRange(files);
                        }
                    }
                }
            }

            return result;
        }

        private List<string> GetBreedAliasesByName(string breed)
        {
            XmlNodeList aliasesList = breeds.SelectNodes(String.Format("//breed[@name='{0}']/aliases/alias", breed));
            List<string> result = new List<string>();
            foreach (XmlElement nn in aliasesList)
            {
                result.Add(nn.InnerText);
            }
            return result;
        }

        private List<string> getRawSources()
        {
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/");
            if (!Directory.Exists(resFolder))
            {
                log("Sources are not found");
                new List<string>();
            }

            var sources = Directory.GetDirectories(resFolder).ToList();
            return sources;
        }

        private void InitUnlinkedBreeds()
        {
            //listBoxUnlinked.Items.Clear();
            List<string> items = getUnlinkedRawBreeds();
            items.Sort();
            listBoxUnlinked.DataSource = items;
        }

        private List<string> getUnlinkedRawBreeds()
        {
            var breeds = getRawBreeds();
            var result = new List<string>();
            foreach (var breed in breeds)
            {
                if (!isLinkedBreed(breed))
                {
                    result.Add(breed);
                }
            }

            return result;
        }

        private List<string> getBreeds()
        {
            List<string> result = new List<string>();
            foreach (XmlElement n in breeds.GetElementsByTagName("breeds")[0].ChildNodes)
            {
                var xmlBreed = n.GetAttribute("name");
                result.Add(xmlBreed);
            }
            return result;
        }

        private bool isLinkedBreed(string breed)
        {
            foreach (XmlElement n in breeds.GetElementsByTagName("breeds")[0].ChildNodes)
            {
                var xmlBreed = n.GetAttribute("name");
                XmlNodeList aliasesList = breeds.SelectNodes(String.Format("//breed[@name='{0}']/aliases/alias", xmlBreed));

                foreach (XmlElement nn in aliasesList)
                {
                    if (nn.InnerText == breed)
                    {
                        return true;
                    }
                }
            }
            return false;            
        }

        private List<string> getRawBreeds()
        {
            List<string> result = new List<string>();
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/");
            if (!Directory.Exists(resFolder))
            {
                return result;
            }

            var sources = Directory.GetDirectories(resFolder);
            foreach (string folder in sources)
            {
                var path = Path.Combine(folder, "breeds");
                if (Directory.Exists(path))
                {
                    var breeds = Directory.GetDirectories(path);
                    foreach (var breed in breeds)
                    {
                        var breedName = new DirectoryInfo(breed).Name;
                        result.Add(breedName);
                    }
                }
            }
            return result;
        }

        private void InitBreeds()
        {
            breedsFile = Path.Combine(textBoxRootPath.Text, "breeds.xml");
            if (!File.Exists(breedsFile))
            {
                XmlDocument doc = new XmlDocument();
                var root = doc.CreateElement("breeds", "urn:1");
                doc.AppendChild(root);
                var txtPath = Path.Combine(textBoxRootPath.Text, "cat_breeds.txt");
                var breeds = File.ReadAllLines(txtPath);
                for (int i = 0; i < breeds.Length; i++)
                {
                    XmlElement n = doc.CreateElement("breed");
                    n.SetAttribute("name", breeds[i]);
                    n.SetAttribute("code", breeds[i].ToLower());
                    root.AppendChild(n);

                    XmlElement aliases = doc.CreateElement("aliases");
                    n.AppendChild(aliases);
                    var breed = breeds[i].ToLower();
                    breed = breed.Replace("-", " ");
                    breed = breed.Replace("_", " ");

                    XmlElement alias1 = doc.CreateElement("alias");
                    alias1.InnerText = breed;
                    aliases.AppendChild(alias1);
                    XmlElement alias2 = doc.CreateElement("alias");
                    alias2.InnerText = breed.Replace(" ", "-");
                    if (alias2.InnerText != breed)
                    {
                        aliases.AppendChild(alias2);
                    }
                    XmlElement alias3 = doc.CreateElement("alias");
                    alias3.InnerText = breed.Replace(" ", "_");
                    if (alias2.InnerText != breed)
                    {
                        aliases.AppendChild(alias3);
                    }
                }
                doc.Save(breedsFile);
            }
            breeds.Load(breedsFile);
        }

        private void log(object data)
        {
            if (listBoxLog.Items.Count > 1000)
            {
                listBoxLog.Items.Clear();
            }
            if (data is Exception)
            {
                listBoxLog.Items.Add((data as Exception).Message);
            }
            else
            {
                listBoxLog.Items.Add(data);
            }
            int visibleItems = listBoxLog.ClientSize.Height / listBoxLog.ItemHeight;
            listBoxLog.TopIndex = Math.Max(listBoxLog.Items.Count - visibleItems + 1, 0);
        }

        private void log(string format, params object[] parameters)
        {
            log((object) String.Format(format, parameters));
        }

        private void buttonCreateFolders_Click(object sender, EventArgs e)
        {
            var fileName = Path.Combine(textBoxRootPath.Text, "cat_breeds.txt");
            var breeds = File.ReadAllLines(fileName);
            var folderPath = "data/breeds/";
            var folderRawPath = "raw/breeds/";
            for (int i = 0; i < breeds.Length; i++)
            {
                var breedPath = Path.Combine(textBoxRootPath.Text, folderPath, breeds[i].ToLower());
                var breedRawPath = Path.Combine(textBoxRootPath.Text, folderRawPath, breeds[i].ToLower());
                if (!Directory.Exists(breedPath))
                {
                    Directory.CreateDirectory(breedPath);
                }
                if (!Directory.Exists(breedRawPath))
                {
                    Directory.CreateDirectory(breedRawPath);
                }
            }
        }

        private void buttonGetBreeds_Click(object sender, EventArgs e)
        {
            bool skip = false;
            if (!String.IsNullOrEmpty(textBoxGoogleStart.Text))
            {
                skip = true;
            }

            var fileName = Path.Combine(textBoxRootPath.Text, "cat_breeds.txt");
            var breeds = File.ReadAllLines(fileName);
            var folderRawPath = "raw/breeds/";
            var se = new GoogleCustomSearch();
            for (int i = 0; i < breeds.Length; i++)
            {
                if (skip)
                {
                    if (breeds[i].ToLower() == textBoxGoogleStart.Text)
                    {
                        skip = false;
                    }
                    else
                    {
                        log("skipping {0}", breeds[i]);
                        continue;
                    }
                }
                var breedRawPath = Path.Combine(textBoxRootPath.Text, folderRawPath, breeds[i].ToLower());
                var images = se.GetImages("cat breed " + breeds[i]);
                foreach (Image img in images)
                {
                    string name = Guid.NewGuid().ToString() + ".jpg";
                    string imgRawPath = Path.Combine(breedRawPath, name);
                    img.Save(imgRawPath, ImageFormat.Jpeg);
                }
            }
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

        private void buttonMurlika_Click(object sender, EventArgs e)
        {
            //lost code ;(
        }

        private void buttonTomall_Click(object sender, EventArgs e)
        {
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/tomall.ru/breeds");
            if (Directory.Exists(resFolder))
            {
                Directory.Delete(resFolder, true);
            }
            Directory.CreateDirectory(resFolder);

            var pagesPath = Path.Combine(textBoxRootPath.Text, "raw/web/tomall.ru/data.csv");

            var pages = File.ReadAllLines(pagesPath);


            foreach (var line in pages)
            {
                var items = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (items.Length == 3)
                {
                    var uri = items[1].Split(new[] { "/", "." }, StringSplitOptions.None);
                    if (uri.Length > 5)
                    {
                        if (uri[5] == "logo") {
                            continue;
                        }
                        var breed = uri[uri.Length - 1].ToLower();
                        log(breed);
                        var breedPath = Path.Combine(resFolder, breed);
                        if (!Directory.Exists(breedPath))
                        {
                            Directory.CreateDirectory(breedPath);
                        }
                        var m = Regex.Match(items[0], "src=\"\"(.+?)\"");
                        var v = m.Groups[1].Value;
                        var nameParts = v.Split(new[] {"\\"}, StringSplitOptions.None);
                        var fileName = nameParts[nameParts.Length - 1];
                        var fullFileName = Path.Combine(textBoxRootPath.Text, "raw/web/tomall.ru/images", fileName);
                        try
                        {
                            var img = new Bitmap(fullFileName, false);
                            if (img != null)
                            {
                                string name = Guid.NewGuid().ToString() + ".jpg";
                                string imgRawPath = Path.Combine(breedPath, name);
                                img.Save(imgRawPath, ImageFormat.Jpeg);
                            }
                            else
                            {
                                log("Not found {0}", fullFileName);
                            }
                        }
                        catch (Exception ex)
                        {
                            log(ex);
                        }
                    }
                }
            }

        }

        private void buttonZoopicture_Click(object sender, EventArgs e)
        {
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/zoopicture.ru/breeds");
            if (Directory.Exists(resFolder))
            {
                Directory.Delete(resFolder, true);
            }
            Directory.CreateDirectory(resFolder);

            var pagesPath = Path.Combine(textBoxRootPath.Text, "raw/web/zoopicture.ru/data.csv");

            var pages = File.ReadAllLines(pagesPath);


            foreach (var line in pages)
            {
                var items = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (items.Length == 4)
                {
                    var uri = items[2].Split(new[] { "/", "." }, StringSplitOptions.None);
                    if (uri.Length > 5)
                    {
                        var breed = uri[5].ToLower();
                        log(breed);
                        var breedPath = Path.Combine(resFolder, breed);
                        if (!Directory.Exists(breedPath))
                        {
                            Directory.CreateDirectory(breedPath);
                        }
                        var ms = Regex.Matches(items[1], "src=\"\"(.+?)\"", RegexOptions.IgnoreCase);
                        foreach (Match m in ms)
                        {
                            var v = m.Groups[1].Value;
                            var nameParts = v.Split(new[] { "\\" }, StringSplitOptions.None);
                            var fileName = nameParts[nameParts.Length - 1];
                            var fullFileName = Path.Combine(textBoxRootPath.Text, "raw/web/zoopicture.ru/img", fileName);
                            try
                            {
                                var img = new Bitmap(fullFileName, false);
                                if (img != null)
                                {
                                    string name = Guid.NewGuid().ToString() + ".jpg";
                                    string imgRawPath = Path.Combine(breedPath, name);
                                    img.Save(imgRawPath, ImageFormat.Jpeg);
                                }
                                else
                                {
                                    log("Not found {0}", fullFileName);
                                }
                            }
                            catch (Exception ex)
                            {
                                log(ex);
                            }
                        }

                    }
                }
            }
        }

        private void buttonMirKo_Click(object sender, EventArgs e)
        {
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/миркошек.рф/breeds");
            if (Directory.Exists(resFolder))
            {
                Directory.Delete(resFolder, true);
            }
            Directory.CreateDirectory(resFolder);

            var pagesPath = Path.Combine(textBoxRootPath.Text, "raw/web/миркошек.рф/data.csv");

            var pages = File.ReadAllLines(pagesPath);


            foreach (var line in pages)
            {
                var items = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (items.Length == 3)
                {
                    var uri = items[1].Split(new[] { "/", "." }, StringSplitOptions.None);
                    if (uri.Length > 5)
                    {
                        var breed = uri[5].ToLower();
                        log(breed);
                        var breedPath = Path.Combine(resFolder, breed);
                        if (!Directory.Exists(breedPath))
                        {
                            Directory.CreateDirectory(breedPath);
                        }
                        var ms = Regex.Matches(items[0], "(https:.+?)\"", RegexOptions.IgnoreCase);
                        foreach (Match m in ms)
                        {
                            var v = m.Groups[1].Value;
                            try
                            {
                                var img = GetImageByURL(v);
                                if (img != null)
                                {
                                    string name = Guid.NewGuid().ToString() + ".jpg";
                                    string imgRawPath = Path.Combine(breedPath, name);
                                    img.Save(imgRawPath, ImageFormat.Jpeg);
                                }
                                else
                                {
                                    log("Not found {0}", v);
                                }
                            }
                            catch (Exception ex)
                            {
                                log(ex);
                            }
                        }
                    }
                }
            }
        }

        private void buttonLoadBreeds_Click(object sender, EventArgs e)
        {
            listBoxBreeds.Items.Clear();
            foreach (XmlElement n in breeds.GetElementsByTagName("breeds")[0].ChildNodes)
            {
                listBoxBreeds.Items.Add(n.GetAttribute("name"));
            }
            listBoxBreeds.Sorted = true;
            groupBoxBreeds.Text = "Breeds collection: " + listBoxBreeds.Items.Count;
        }

        private void listBoxBreeds_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxBreeds.SelectedIndex >= 0)
            {
                ShowAliases();
            }
        }

        private void ShowAliases()
        {
            List<string> lines = new List<string>();
            XmlNodeList aliasesList = breeds.SelectNodes(String.Format("//breed[@name='{0}']/aliases/alias", listBoxBreeds.SelectedItem.ToString()));

            foreach (XmlElement n in aliasesList)
            {
                lines.Add(n.InnerText);
            }
            textBoxBreedAlias.Lines = lines.ToArray();
        }

        private void buttonApplyBreed_Click(object sender, EventArgs e)
        {
            if (listBoxBreeds.SelectedItem == null)
            {
                return;
            }
            XmlNodeList targets = breeds.SelectNodes(String.Format("//breed[@name='{0}']/aliases", listBoxBreeds.SelectedItem.ToString()));
            if (targets.Count == 1)
            {
                targets[0].RemoveAll();
                foreach (string line in textBoxBreedAlias.Lines)
                {
                    XmlElement alias = breeds.CreateElement("alias");
                    alias.InnerText = line;
                    targets[0].AppendChild(alias);
                }
                breeds.Save(breedsFile);
            }

        }

        private void listBoxUnlinked_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxBreeds.SelectedIndex == -1)
            {
                log("Please select breed!");
                return;
            }
            var index = listBoxBreeds.SelectedIndex;
            var alias = listBoxUnlinked.SelectedValue.ToString();
            textBoxBreedAlias.Text += String.Format("{0}{1}", Environment.NewLine, alias);
            buttonApplyBreed_Click(null, null);
            buttonLoadBreeds_Click(null, null);
            InitUnlinkedBreeds();
            listBoxBreeds.SelectedIndex = index;
            //var data = (listBoxUnlinked.DataSource as List<string>);
            //data.RemoveAt(listBoxUnlinked.SelectedIndex);
            //listBoxUnlinked.DataSource = null;
            //listBoxUnlinked.Items.Clear();
            //listBoxUnlinked.DataSource = data;
            //List<string>
            //listBoxUnlinked.Items.RemoveAt(listBoxUnlinked.SelectedIndex);
        }

        private void listBoxUnlinked_SelectedValueChanged(object sender, EventArgs e)
        {
            var breed = listBoxUnlinked.SelectedValue.ToString();
            breed = breed.Replace("_", " ");
            breed = breed.Replace("-", " ");
            breed = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(breed.ToLower());
            textBoxBreedNewName.Text = breed;

            CalcSuggest(breed);
        }

        private string prepareToCmp(string data)
        {
            return data.ToLower().Replace("_", "").Replace("-", "").Replace(",", "").Replace("cat", "").Replace("koshka", " ").Replace(" ", "");
        }

        private string CalcSuggest(string breed)
        {
            breed = prepareToCmp(breed);
            int min = 1000;
            int max = 0;
            string suggest1 = "";
            string suggest2 = "";
            foreach (XmlElement n in breeds.GetElementsByTagName("breeds")[0].ChildNodes)
            {
                var xmlBreed = n.GetAttribute("name");
                XmlNodeList aliasesList = breeds.SelectNodes(String.Format("//breed[@name='{0}']/aliases/alias", xmlBreed));

                foreach (XmlElement nn in aliasesList)
                {
                    var breed2cmp = prepareToCmp(nn.InnerText);
                    var l1 = LevenshteinDistance.Compute(breed, breed2cmp);
                    var l2 = LevenshteinDistance.LongestCommonSubstring(breed, breed2cmp);
                    if (l1 < min)
                    {
                        min = l1;
                        suggest1 = xmlBreed;
                    }
                    if (l2 > max)
                    {
                        max = l2;
                        suggest2 = xmlBreed;
                    }
                }
            }
            if (suggest1 == suggest2)
            {
                log("Suggest: {0} ({1}:{2})", suggest1, min, max);
                return suggest1;
            }
            else
            {
                log("maybe: {0} ({1}) or {2} ({3})", suggest1, min, suggest2, max);
            }

            return string.Empty;
        }


        private void buttonAddBreed_Click(object sender, EventArgs e)
        {
            XmlElement n = breeds.CreateElement("breed");
            n.SetAttribute("name", textBoxBreedNewName.Text);
            n.SetAttribute("code", textBoxBreedNewName.Text.ToLower());
            breeds.DocumentElement.AppendChild(n);

            XmlElement aliases = breeds.CreateElement("aliases");
            n.AppendChild(aliases);
            XmlElement alias = breeds.CreateElement("alias");
            alias.InnerText = textBoxBreedNewName.Text.ToLower().ToLower();
            aliases.AppendChild(alias);

            breeds.Save(breedsFile);
            buttonLoadBreeds_Click(null, null);
            InitUnlinkedBreeds();
        }

        private void BreedDelete_Click(object sender, EventArgs e)
        {
            XmlNodeList targets = breeds.SelectNodes(String.Format("//breed[@name='{0}']", listBoxBreeds.SelectedItem.ToString()));
            if (targets.Count > 0)
            {
                targets[0].ParentNode.RemoveChild(targets[0]);
                breeds.Save(breedsFile);
                buttonLoadBreeds_Click(null, null);
            }
        }

        private void buttonZooGlo_Click(object sender, EventArgs e)
        {
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/zooglobal.ru/breeds");
            if (Directory.Exists(resFolder))
            {
                Directory.Delete(resFolder, true);
            }
            Directory.CreateDirectory(resFolder);

            var pagesPath = Path.Combine(textBoxRootPath.Text, "raw/web/zooglobal.ru/data.csv");

            var pages = File.ReadAllLines(pagesPath);


            foreach (var line in pages)
            {
                var items = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (items.Length == 4)
                {
                    var uri = "http://zooglobal.ru" + items[1];
                    //if (uri.Length > 3)
                    {
                        var breed = items[0].Replace("\"","");
                        log(breed);
                        var breedPath = Path.Combine(resFolder, breed);
                        if (!Directory.Exists(breedPath))
                        {
                            Directory.CreateDirectory(breedPath);
                        }

                        try
                        {
                            var img = GetImageByURL(uri);
                            if (img != null)
                            {
                                string name = Guid.NewGuid().ToString() + ".jpg";
                                string imgRawPath = Path.Combine(breedPath, name);
                                img.Save(imgRawPath, ImageFormat.Jpeg);
                            }
                            else
                            {
                                log("Not found {0}", uri);
                            }
                        }
                        catch (Exception ex)
                        {
                            log(ex);
                        }
                    }
                }
            }
        }

        private void listBoxUnlinked_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var breed = listBoxUnlinked.SelectedValue.ToString();
                breed = breed.Replace("_", " ");
                breed = breed.Replace("-", " ");
                breed = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(breed.ToLower());
                textBoxBreedNewName.Text = breed;

                var suggest = CalcSuggest(breed);
                if (!String.IsNullOrEmpty(suggest))
                {
                    listBoxBreeds.SelectedIndex = listBoxBreeds.Items.IndexOf(suggest);
                }
            }
        }

        private void buttonCatTomsk_Click(object sender, EventArgs e)
        {
            //lost code :(
        }

        private void buttonVashiPets_Click(object sender, EventArgs e)
        {
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/vashipitomcy.ru/breeds");
            if (Directory.Exists(resFolder))
            {
                Directory.Delete(resFolder, true);
            }
            Directory.CreateDirectory(resFolder);

            var pagesPath = Path.Combine(textBoxRootPath.Text, "raw/web/vashipitomcy.ru/data.csv");

            var pages = File.ReadAllLines(pagesPath);


            foreach (var line in pages)
            {
                var items = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (items.Length == 3)
                {
                    var uri = items[1].Split(new[] { "/", "." }, StringSplitOptions.None);
                    if (uri.Length > 6)
                    {
                        var breed = uri[6].ToLower();
                        if (breed.Contains("porody_koshek"))
                        {
                            log("skipped {0}", breed);
                            continue;
                        }
                        log(breed);
                        var breedPath = Path.Combine(resFolder, breed);
                        if (!Directory.Exists(breedPath))
                        {
                            Directory.CreateDirectory(breedPath);
                        }
                        var m = Regex.Match(items[0], "src=\"\"(.+?)\"");
                        var v = m.Groups[1].Value;
                        var nameParts = v.Split(new[] { "\\" }, StringSplitOptions.None);
                        var fileName = nameParts[nameParts.Length - 1];
                        var fullFileName = Path.Combine(textBoxRootPath.Text, "raw/web/vashipitomcy.ru/images", fileName);

                        try
                        {
                            var img = GetImageByURL(v);
                            if (img != null)
                            {
                                string name = Guid.NewGuid().ToString() + ".jpg";
                                string imgRawPath = Path.Combine(breedPath, name);
                                img.Save(imgRawPath, ImageFormat.Jpeg);
                            }
                            else
                            {
                                log("Not found {0}", uri);
                            }
                        }
                        catch (Exception ex)
                        {
                            log(ex);
                        }
                    }
                }
            }
        }

        private void buttonReloadUnlinked_Click(object sender, EventArgs e)
        {
            InitUnlinkedBreeds();
        }

        private void buttonMurkote_Click(object sender, EventArgs e)
        {
            var src = "murkote.com";
            var resFolder = Path.Combine(textBoxRootPath.Text, "raw/web/"+src+"/breeds");
            if (Directory.Exists(resFolder))
            {
                Directory.Delete(resFolder, true);
            }
            Directory.CreateDirectory(resFolder);
            var pagesPath = Path.Combine(textBoxRootPath.Text, "raw/web/"+src+"/data.csv");
            var pages = File.ReadAllLines(pagesPath);
            foreach (var line in pages)
            {
                var items = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (items.Length == 3)
                {
                    var uri = items[1].Split(new[] { "/", "." }, StringSplitOptions.None);
                    if (uri.Length > 4)
                    {
                        var breed = uri[4].ToLower();
                        log(breed);
                        var breedPath = Path.Combine(resFolder, breed);
                        if (!Directory.Exists(breedPath))
                        {
                            Directory.CreateDirectory(breedPath);
                        }
                        var v = items[0].Replace("\"http","http");
                        try
                        {
                            var img = GetImageByURL(v);
                            if (img != null)
                            {
                                string name = Guid.NewGuid().ToString() + ".jpg";
                                string imgRawPath = Path.Combine(breedPath, name);
                                img.Save(imgRawPath, ImageFormat.Jpeg);
                            }
                            else
                            {
                                log("Not found {0}", uri);
                            }
                        }
                        catch (Exception ex)
                        {
                            log(ex);
                        }
                    }
                }
            }
        }

        private void comboBoxBreed_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateImages();
        }

        private void comboBoxSrc_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateImages();
        }

        private void trackBarSize_Scroll(object sender, EventArgs e)
        {
            listViewImages.LargeImageList.ImageSize = new Size(trackBarSize.Value, trackBarSize.Value);
            listViewImages.Refresh();
        }

        private void trackBarSize_MouseUp(object sender, MouseEventArgs e)
        {
            UpdateImages();
        }

        private void listViewImages_DoubleClick(object sender, EventArgs e)
        {
            
            
        }

        private void SetImageStatus(string fileName, bool approved)
        {
            var listToAdd = approved ? ImagesApproved : ImagesDeclined;
            var listToRemove = approved ? ImagesDeclined : ImagesApproved;

            if (!listToAdd.Contains(fileName))
            {
                listToAdd.Add(fileName);
            }
            if (listToRemove.Contains(fileName))
            {
                listToRemove.RemoveAt(listToRemove.IndexOf(fileName));
            }
            SaveImageLists();
        }

        private void SaveImageLists()
        {
            try
            {
                File.WriteAllLines(ImagesApprovedPath, ImagesApproved, Encoding.UTF8);
                File.WriteAllLines(ImagesDeclinedPath, ImagesDeclined, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                log(ex);
            }
        }

        private void listViewImages_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            UpdateImageItems();

        }

        private void UpdateImageItems()
        {
            foreach (var item in listViewImages.Items.Cast<ListViewItem>())
            {
                if (item == null)
                {
                    continue;
                }
                bool approved = ImagesApproved.Contains(item.Text);
                bool declined = ImagesDeclined.Contains(item.Text);

                if (approved)
                {
                    item.BackColor = Color.Green;
                }
                if (declined)
                {
                    item.BackColor = Color.Gray;
                }
                if (approved && declined)
                {
                    item.BackColor = Color.Red;
                    log("There is an error in Approve lists: {0}", item.Text);
                }
            }
        }

        private void listViewImages_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            bool status = (e.Button == MouseButtons.Left);

            if (listViewImages.SelectedItems.Count == 1)
            {
                ListView.SelectedListViewItemCollection items = listViewImages.SelectedItems;
                ListViewItem lvItem = items[0];
                SetImageStatus(lvItem.Text, status);
                UpdateImageItems();
                if (status)
                {
                    log("Approved! ({0})", lvItem.Text);
                }
                else
                {
                    log("Declined! ({0})", lvItem.Text);
                }
            }
        }

        private void listViewImages_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == 'e')
            {
                bool status = e.KeyChar == ' ';

                if (listViewImages.SelectedItems.Count == 1)
                {
                    ListView.SelectedListViewItemCollection items = listViewImages.SelectedItems;
                    ListViewItem lvItem = items[0];
                    SetImageStatus(lvItem.Text, status);
                    UpdateImageItems();
                    if (status)
                    {
                        log("Approved! ({0})", lvItem.Text);
                    }
                    else
                    {
                        log("Declined! ({0})", lvItem.Text);
                    }
                }

                e.Handled = true;
            }
        }

        private void buttonSearchDuplicates_Click(object sender, EventArgs e)
        {
            SearchForDuplicates2();
        }

        private void SearchForDuplicates2()
        {
            Duplicates.Clear();
            var files = GetImages();
            var images = GetComparableImages(files);

            for (int i = 0; i < files.Count; i++)
            {
                if (DuplicatesContainsItem(files[i]))
                {
                    continue;
                }
                log("Searching for duplicates {0} of {1}", i, files.Count);
                for (int j = i + 1; j < files.Count; j++)
                {
                    if (IsSimilarImages(images[files[i]], images[files[j]]))
                    {
                        if (!Duplicates.ContainsKey(files[i]))
                        {
                            Duplicates.Add(files[i], new List<string>());
                            Duplicates[files[i]].Add(files[i]);
                        }
                        Duplicates[files[i]].Add(files[j]);
                        if (Duplicates.Count > 1)
                        {
                            //ShowDuplicates();
                            //return;
                        }
                    }
                }
            }
            ShowDuplicates();
        }

        private bool DuplicatesContainsItem(string file)
        {
            foreach (KeyValuePair<string, List<string>> item in Duplicates)
            {
                if (item.Value.Contains(file))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSimilarImages(ComparableImage i1, ComparableImage i2)
        {
            double similarity = i1.CalculateSimilarity(i2);
            return similarity * 100 >= trackBarSimilarity.Value;
        }

        private Dictionary<string, ComparableImage> GetComparableImages(List<string> files)
        {
            var result = new Dictionary<string, ComparableImage>();
            foreach (string file in files)
            {
                result.Add(file, new ComparableImage(new FileInfo(file)));
            }
            return result;
        }

        private void SearchForDuplicates1()
        {
            var files = GetImages();
            for (int i = 0; i < files.Count; i++)
            {
                log("Searching for duplicates {0} of {1}", i, files.Count);
                for (int j = i + 1; j < files.Count; j++)
                {
                    if (IsSimilarImages(files[i], files[j]))
                    {
                        if (!Duplicates.ContainsKey(files[i]))
                        {
                            Duplicates.Add(files[i], new List<string>());
                        }
                        Duplicates[files[i]].Add(files[j]);
                        if (Duplicates.Count > 1)
                        {
                            //ShowDuplicates();
                            //return;
                        }
                    }
                }
            }
            ShowDuplicates();
        }

        private void ShowDuplicates()
        {
            var size = new Size(100, 100);// new Size(trackBarSize.Value, trackBarSize.Value);
            listViewDuplicates.Clear();
            // create image list and fill it 

            var imageList = new ImageList();
            imageList.ImageSize = size;
            imageList.ColorDepth = ColorDepth.Depth16Bit;
            int index = 0;
            foreach (KeyValuePair<string, List<string>> file in Duplicates)
            {
                var item = new ListViewImageItem(file.Key);
                Bitmap img;
                try
                {
                    img = new Bitmap(file.Key);
                }
                catch (Exception ex)
                {
                    img = new Bitmap(size.Width, size.Height);
                }
                img = FitImg(img, size);
                imageList.Images.Add(img);
                listViewDuplicates.Items.Add(item.Path, index++);
            }
            if (imageList.Images.Count > 0)
            {
                listViewDuplicates.LargeImageList = imageList;
                listViewDuplicates.SmallImageList = imageList;
            }
            //UpdateImageItems();
        }

        private bool IsSimilarImages(string path1, string path2)
        {
            double similarity = GetSimilarity(path1, path2);
            //log("similarity {0} and {1} = {2}", path1, path2, similarity);
            return similarity * 100 >= trackBarSimilarity.Value;
        }

        private double GetSimilarity(string path1, string path2)
        {
            try
            {
                ComparableImage i1 = new ComparableImage(new FileInfo(path1));
                ComparableImage i2 = new ComparableImage(new FileInfo(path2));
                return i1.CalculateSimilarity(i2);
            }
            catch (Exception ex)
            {
                log("cannot get similarity for {0} and {1}", path1, path2);
                return 0;
            }
        }

        private void trackBarSimilarity_Scroll(object sender, EventArgs e)
        {
            labelSimilarity.Text = trackBarSimilarity.Value.ToString();
        }

        private void buttonZero_Click(object sender, EventArgs e)
        {
            var files = GetImages();
            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    var info = new FileInfo(files[i]);
                    if (info.Length == 0)
                    {
                        log("Deleting 0 bite file {0}", files[i]);
                        File.Delete(files[i]);
                    }
                }
                catch (Exception ex)
                {
                    log(ex);
                }
            }
            ShowDuplicates();
        }

        private void listViewDuplicates_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawDuplicateItems();
        }

        private void DrawDuplicateItems()
        {
            if (listViewDuplicates.SelectedIndices.Count == 1)
            {
                var items = Duplicates[listViewDuplicates.SelectedItems[0].Text];
                var size = new Size(100, 100);// new Size(trackBarSize.Value, trackBarSize.Value);
                listViewDuplicateItems.Clear();
                // create image list and fill it 

                var imageList = new ImageList();
                imageList.ImageSize = size;
                imageList.ColorDepth = ColorDepth.Depth16Bit;
                int index = 0;
                foreach (string file in items)
                {
                    var item = new ListViewImageItem(file);
                    Bitmap img;
                    try
                    {
                        img = new Bitmap(file);
                    }
                    catch (Exception ex)
                    {
                        img = new Bitmap(size.Width, size.Height);
                    }
                    img = FitImg(img, size);
                    imageList.Images.Add(img);
                    listViewDuplicateItems.Items.Add(item.Path, index++);
                }
                listViewDuplicateItems.LargeImageList = imageList;
                listViewDuplicateItems.SmallImageList = imageList;
            }
        }

        private void listViewDuplicateItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewDuplicateItems.SelectedItems.Count == 1)
            {
                var item = listViewDuplicateItems.SelectedItems[0];
                pictureBoxDuplicate.ImageLocation = item.Text;
            }
        }

        private void checkBoxHideReviewed_CheckedChanged(object sender, EventArgs e)
        {
            UpdateImages();
        }
    }
}
