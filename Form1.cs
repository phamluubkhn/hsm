using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using EasySign.Core.New.Demo.SigningAPI;

namespace HsmTool
{
    public partial class Form1 : Form
    {
        private static string xmlText = "";
        public Form1()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = false;
            openFileDialog.AddExtension = true;
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "XML files (*.xml)|*.xml";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                label3.Text = openFileDialog.FileName;
                xmlText = File.ReadAllText(openFileDialog.FileName);
            }

            
        }

        private async void button2_Click(object sender, EventArgs e)
        {

            try
            {
                await ProcessSignHsm();
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.ToString();
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            try
            {
                await ProcessSignHsmV2();
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.ToString();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);
                byte[] dataFile = Encoding.UTF8.GetBytes(doc.OuterXml);
                var xmlBase64 = Convert.ToBase64String(dataFile);
                string url = ConfigurationManager.AppSettings["softDreamUrl"];
                string user = ConfigurationManager.AppSettings["softDreamUser"];
                string pass = ConfigurationManager.AppSettings["softDreamPass"];
                SigningAPI signingAPI = new SigningAPI(url, user, pass);
                XmlNodeList xmlNodeList = doc.GetElementsByTagName("DLTKhai");
                string trans = xmlNodeList[0].Attributes["Id"].Value;
                string text = signingAPI.SignXML635Misa(textBox1.Text, textBox2.Text, doc.OuterXml, null, trans, "TKhai/DLTKhai", "TKhai/DSCKS");
                textBox3.Text = text;
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.ToString();
            }
        }

        private async Task ProcessSignHsm()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);
            byte[] dataFile = Encoding.UTF8.GetBytes(doc.InnerXml);
            var xmlBase64 = Convert.ToBase64String(dataFile);
            string cyberuRL = ConfigurationManager.AppSettings["cyberuRL"];
            using (var client = HttpClientFactory.CreateHttpClient(cyberuRL, textBox1.Text, textBox2.Text))
            {
                var option = new { base64xml = xmlBase64, hashalg = "SHA1" };
                var payload = JsonSerializer.Serialize(option);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var res = await client.PostAsync(cyberuRL, content);
                var result = await res.Content.ReadAsStringAsync();
                XmlSignedData xmlSignedData = JsonSerializer.Deserialize<XmlSignedData>(result);
                string text =  UTF8Encoding.UTF8.GetString(Convert.FromBase64CharArray(xmlSignedData.base64xmlsigned.ToCharArray(), 0, xmlSignedData.base64xmlsigned.Length));
                textBox3.Text = text;
                
            }
        }

        private async Task ProcessSignHsmV2()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);
            byte[] dataFile = Encoding.UTF8.GetBytes(doc.InnerXml);
            var xmlBase64 = Convert.ToBase64String(dataFile);
            string cyberuRL = ConfigurationManager.AppSettings["cyberuRL"];
            using (var client = HttpClientFactory.CreateHttpClient(cyberuRL, textBox1.Text, textBox2.Text))
            {
                XmlNodeList xmlNodeListDLTKhai = doc.GetElementsByTagName("DLTKhai");
                XmlNodeList xmlNodeListDSCKS = doc.GetElementsByTagName("DSCKS");
                var option = new { base64xml = xmlBase64, hashalg = "SHA1", xpathdata= "/TKhai/DLTKhai", xpathsign= "/TKhai/DSCKS" };
                var payload = JsonSerializer.Serialize(option);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var res = await client.PostAsync(cyberuRL, content);
                var result = await res.Content.ReadAsStringAsync();
                XmlSignedData xmlSignedData = JsonSerializer.Deserialize<XmlSignedData>(result);
                string text = UTF8Encoding.UTF8.GetString(Convert.FromBase64CharArray(xmlSignedData.base64xmlsigned.ToCharArray(), 0, xmlSignedData.base64xmlsigned.Length));
                textBox3.Text = text;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);
                byte[] dataFile = Encoding.UTF8.GetBytes(doc.OuterXml);
                var xmlBase64 = Convert.ToBase64String(dataFile);
                string url = ConfigurationManager.AppSettings["softDreamUrl"];
                string user = ConfigurationManager.AppSettings["softDreamUser"];
                string pass = ConfigurationManager.AppSettings["softDreamPass"];
                SigningAPI signingAPI = new SigningAPI(url, user, pass);
                XmlNodeList xmlNodeList = doc.GetElementsByTagName("DLHDon");
                string trans = xmlNodeList[0].Attributes["Id"].Value;
                string text = signingAPI.SignXML635Misa(textBox1.Text, textBox2.Text, doc.OuterXml, null, trans, "HDon/DSCKS/NBan", "/HDon/DSCKS");
                //string text = signingAPI.SignXML635(textBox1.Text, textBox2.Text, doc.OuterXml, null, null);
                textBox3.Text = text;
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.ToString();
            }
        }
    }
}
